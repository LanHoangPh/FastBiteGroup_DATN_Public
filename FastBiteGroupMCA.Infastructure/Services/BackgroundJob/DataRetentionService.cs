using FastBiteGroupMCA.Application.IServices.BackgroundJob;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;

namespace FastBiteGroupMCA.Infastructure.Services.BackgroundJob;

public class DataRetentionService : IDataRetentionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagesRepository _messagesRepository;
    private readonly ILogger<DataRetentionService> _logger;
    private readonly StorageStrategy _storageStrategy;

    public DataRetentionService(
           IUnitOfWork unitOfWork, 
           ILogger<DataRetentionService> logger, 
           IMessagesRepository messagesRepository, 
           StorageStrategy storageStrategy)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _messagesRepository = messagesRepository;
        _storageStrategy = storageStrategy;
    }

    public async Task CleanUpOrphanedPostFilesAsync()
    {
        _logger.LogInformation("Starting orphaned post files cleanup job...");

        var cutoffTime = DateTime.UtcNow.AddHours(-24);

        // 1. Tìm các file được upload cho bài viết, đã cũ, và không có liên kết nào trong PostAttachment
        var orphanedFiles = await _unitOfWork.SharedFiles.GetQueryable()
            .Where(f =>
                f.FileContext == "PostAttachment" &&
                f.UploadedAt < cutoffTime &&
                !_unitOfWork.PostAttachments.GetQueryable().Any(pa => pa.FileID == f.FileID)
            )
            .ToListAsync();

        if (!orphanedFiles.Any())
        {
            _logger.LogInformation("No orphaned post files found to clean up.");
            return;
        }

        _logger.LogInformation("Found {Count} orphaned files to delete.", orphanedFiles.Count);

        foreach (var file in orphanedFiles)
        {
            try
            {

                var storageService = _storageStrategy.GetStorageService(file.FileType ?? "application/octet-stream");
                var deletedFromStorage = await storageService.DeleteAsync(file.StorageUrl);

                if (deletedFromStorage)
                {
                    _unitOfWork.SharedFiles.Remove(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting orphaned file {FileId} with URL {Url}", file.FileID, file.StorageUrl);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Orphaned post files cleanup job finished.");
    }

    public async Task PermanentlyDeleteGroupAsync(Guid groupId)
    {
        _logger.LogInformation("Starting permanent deletion for group {GroupId}", groupId);

        // --- BƯỚC 1: Lấy ID của các thực thể con chính (Posts, Conversations) ---
        var postIds = await _unitOfWork.Posts.GetQueryable()
            .Where(p => p.GroupID == groupId)
            .Select(p => p.PostID)
            .ToListAsync();

        var conversationIds = await _unitOfWork.Conversations.GetQueryable()
            .Where(c => c.ExplicitGroupID == groupId)
            .Select(c => c.ConversationID)
            .ToListAsync();

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null)
            {
                _logger.LogWarning("Group {GroupId} was already deleted before the job started.", groupId);
                await transaction.CommitAsync(); // Vẫn commit để hoàn tất transaction trống
                return;
            }

            // Xóa Likes và Comments của các Posts
            if (postIds.Any())
            {
                await _unitOfWork.PostLikes.GetQueryable().Where(l => postIds.Contains(l.PostID)).ExecuteDeleteAsync();
                await _unitOfWork.PostComments.GetQueryable().Where(c => postIds.Contains(c.PostID)).ExecuteDeleteAsync();
                // Xóa các file đính kèm liên quan đến post (nếu có)
                await _unitOfWork.PostAttachments.GetQueryable().Where(pa => postIds.Contains(pa.PostID)).ExecuteDeleteAsync();
            }

            // Xóa Participants, Polls, VideoCalls của các Conversations
            if (conversationIds.Any())
            {
                await _unitOfWork.ConversationParticipants.GetQueryable().Where(p => conversationIds.Contains(p.ConversationID)).ExecuteDeleteAsync();

                var pollIds = await _unitOfWork.Polls.GetQueryable().Where(p => conversationIds.Contains(p.ConversationID)).Select(p => p.PollID).ToListAsync();
                if (pollIds.Any())
                {
                    await _unitOfWork.PollVotes.GetQueryable().Where(v => pollIds.Contains(v.PollOption.PollID)).ExecuteDeleteAsync();
                    await _unitOfWork.PollOptions.GetQueryable().Where(o => pollIds.Contains(o.PollID)).ExecuteDeleteAsync();
                    await _unitOfWork.Polls.GetQueryable().Where(p => pollIds.Contains(p.PollID)).ExecuteDeleteAsync();
                }

                var videoCallSessionIds = await _unitOfWork.VideoCallSessions.GetQueryable().Where(v => conversationIds.Contains(v.ConversationID)).Select(v => v.VideoCallSessionID).ToListAsync();
                if (videoCallSessionIds.Any())
                {
                    await _unitOfWork.VideoCallParticipants.GetQueryable().Where(p => videoCallSessionIds.Contains(p.VideoCallSessionID)).ExecuteDeleteAsync();
                    await _unitOfWork.VideoCallSessions.GetQueryable().Where(v => videoCallSessionIds.Contains(v.VideoCallSessionID)).ExecuteDeleteAsync();
                }
            }

            // Xóa các thực thể con trực tiếp của Group
            await _unitOfWork.Posts.GetQueryable().Where(p => p.GroupID == groupId).ExecuteDeleteAsync();
            await _unitOfWork.Conversations.GetQueryable().Where(c => c.ExplicitGroupID == groupId).ExecuteDeleteAsync();
            await _unitOfWork.GroupMembers.GetQueryable().Where(m => m.GroupID == groupId).ExecuteDeleteAsync();
            await _unitOfWork.UserGroupInvitations.GetQueryable().Where(i => i.GroupID == groupId).ExecuteDeleteAsync();
            await _unitOfWork.GroupInvitations.GetQueryable().Where(i => i.GroupID == groupId).ExecuteDeleteAsync();
            await _unitOfWork.ContentReports.GetQueryable().Where(r => r.GroupID == groupId).ExecuteDeleteAsync();

            // Xóa bản thân Group
            // Dùng Remove() cho đối tượng đã lấy ra ban đầu
            _unitOfWork.Groups.Remove(group);
            await _unitOfWork.SaveChangesAsync();

            // Hoàn tất giao dịch
            await transaction.CommitAsync();
            _logger.LogInformation("Successfully permanently deleted group {GroupId} and its related data.", groupId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to permanently delete group {GroupId}", groupId);
            throw;
        }
        try
        {
            if (conversationIds.Any())
            {
                var deletedCount = await _messagesRepository.DeleteManyByConversationIdsAsync(conversationIds);
                _logger.LogInformation("Deleted {Count} messages from MongoDB for group {GroupId}", deletedCount, groupId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete MongoDB messages for group {GroupId}. This job will be retried by Hangfire.", groupId);
            throw; 
        }

        _logger.LogInformation("Successfully and completely deleted group {GroupId}", groupId);
    }

    public async Task PurgeOldAuditLogsAsync()
    {
        // Xóa các log cũ hơn 1 năm
        var threshold = DateTime.UtcNow.AddYears(-1);

        _logger.LogInformation("Starting job to purge audit logs older than {ThresholdDate}", threshold);

        var rowsAffected = await _unitOfWork.AdminAuditLogs.GetQueryable()
            .Where(log => log.Timestamp < threshold)
            .ExecuteDeleteAsync();

        _logger.LogWarning("Successfully purged {Count} old audit log entries.", rowsAffected);
    }

    public async Task PurgeRevokedRefreshTokensAsync()
    {
        // Xóa các refresh token đã bị thu hồi và hết hạn
        var now = DateTime.UtcNow;
        _logger.LogInformation("Starting job to purge revoked and expired refresh tokens.");
        var rowsAffected = await _unitOfWork.RefreshToken.GetQueryable()
            .Where(token => token.IsRevoked && token.ExpiresAt < now)
            .ExecuteDeleteAsync();
        _logger.LogWarning("Successfully purged {Count} revoked and expired refresh tokens.", rowsAffected);
    }

    public async Task PurgeSoftDeletedGroupsAsync()
    {
        // Xóa các nhóm đã được đánh dấu xóa mềm hơn 10 ngày trước
        var threshold = DateTime.UtcNow.AddDays(-10);

        _logger.LogInformation("Starting job to purge soft-deleted groups older than {ThresholdDate}", threshold);

        var groupsToDelete = await _unitOfWork.Groups.GetQueryable()
            .Where(g => g.IsDeleted && g.UpdatedAt < threshold)
            .ToListAsync();

        if (!groupsToDelete.Any())
        {
            _logger.LogInformation("No soft-deleted groups found to purge.");
            return;
        }

        _unitOfWork.Groups.RemoveRange(groupsToDelete);
        var affectedRows = await _unitOfWork.SaveChangesAsync();

        _logger.LogWarning("Successfully purged {Count} soft-deleted groups.", affectedRows);
    }

    public async Task PurgeSoftDeletedUsersAsync()
    {
        var threshold = DateTime.UtcNow.AddDays(-30);
        _logger.LogInformation("Starting job to purge soft-deleted users older than {ThresholdDate}", threshold);
        var usersToDelete = await _unitOfWork.Users.GetQueryable()
            .Where(u => u.IsDeleted && u.UpdatedAt < threshold)
            .ToListAsync();
        if (!usersToDelete.Any())
        {
            _logger.LogInformation("No soft-deleted users found to purge.");
            return;
        }
        _unitOfWork.Users.RemoveRange(usersToDelete);
        var affectedRows = await _unitOfWork.SaveChangesAsync();
        _logger.LogWarning("Successfully purged {Count} soft-deleted users.", affectedRows);
    }
    public async Task PermanentlyDeleteOldRecalledMessagesAsync()
    {
        _logger.LogInformation("Starting old recalled messages cleanup job...");
        var cutoffTime = DateTime.UtcNow.AddDays(-30);

        var messagesToDelete = await _messagesRepository.GetOldRecalledMessagesAsync(cutoffTime);

        if (!messagesToDelete.Any())
        {
            _logger.LogInformation("No old recalled messages found to delete.");
            return;
        }

        _logger.LogInformation("Found {Count} recalled messages to permanently delete.", messagesToDelete.Count);

        // --- Phần còn lại của logic của bạn đã rất tốt và được giữ nguyên ---

        // Thu thập ID của các file đính kèm liên quan
        var fileIdsToDelete = messagesToDelete
            .Where(m => m.Attachments != null)
            .SelectMany(m => m.Attachments!.Select(a => a.FileId))
            .ToList();

        if (fileIdsToDelete.Any())
        {
            // Xóa các file trên Cloud Storage và trong CSDL SQL
            var sharedFiles = await _unitOfWork.SharedFiles.GetQueryable()
                .Where(f => fileIdsToDelete.Contains(f.FileID)).ToListAsync();

            foreach (var file in sharedFiles)
            {
                var storageService = _storageStrategy.GetStorageService(file.FileType ?? "");
                await storageService.DeleteAsync(file.StorageUrl);
            }

            _unitOfWork.SharedFiles.RemoveRange(sharedFiles);
            await _unitOfWork.SaveChangesAsync();
        }

        // Cuối cùng, xóa vĩnh viễn các document tin nhắn khỏi MongoDB
        var messageIdsToDelete = messagesToDelete.Select(m => m.Id).ToList();
        var deletedCount = await _messagesRepository.DeleteManyByIdsAsync(messageIdsToDelete);

        _logger.LogInformation("Permanently deleted {Count} message documents from MongoDB.", deletedCount);
        _logger.LogInformation("Old recalled messages cleanup job finished.");
    }

    public async Task DeleteOldLoginHistoryAsync()
    {
        // Giữ lại lịch sử trong 90 ngày
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        _logger.LogInformation("Deleting login history records older than {CutoffDate}", cutoffDate);

        var deletedCount = await _unitOfWork.LoginHistories.GetQueryable()
            .Where(h => h.LoginTimestamp < cutoffDate)
            .ExecuteDeleteAsync();

        _logger.LogInformation("Successfully deleted {Count} old login history records.", deletedCount);
    }

    public async Task PermanentlyDeleteConversationAsync(int conversationId)
    {
        _logger.LogInformation("Starting permanent deletion for conversation {ConversationId}", conversationId);

        var activeParticipants = await _unitOfWork.ConversationParticipants.GetQueryable()
            .CountAsync(p => p.ConversationID == conversationId && !p.IsArchived);

        if (activeParticipants > 0)
        {
            _logger.LogWarning("Aborting deletion for conversation {ConversationId} because a participant has un-archived it.", conversationId);
            return;
        }

        // 1. Xóa tất cả tin nhắn trong MongoDB
        await _messagesRepository.DeleteManyByConversationIdsAsync(new List<int> { conversationId });

        // 2. Xóa các bản ghi liên quan trong SQL (trong transaction)
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.ConversationParticipants.GetQueryable()
                .Where(p => p.ConversationID == conversationId).ExecuteDeleteAsync();

            await _unitOfWork.Conversations.GetQueryable()
                .Where(c => c.ConversationID == conversationId).ExecuteDeleteAsync();

            await transaction.CommitAsync();
            _logger.LogInformation("Successfully and permanently deleted conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to permanently delete conversation {ConversationId} from SQL.", conversationId);
            throw;
        }
    }
}
