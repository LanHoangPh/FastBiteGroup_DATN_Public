using System.ComponentModel;

namespace FastBiteGroupMCA.Application.IServices.BackgroundJob;

public interface IDataRetentionService
{
    /// <summary>
    /// Xóa vĩnh viễn các nhóm đã được xóa mềm quá một khoảng thời gian.
    /// </summary>
    Task PurgeSoftDeletedGroupsAsync();

    /// <summary>
    /// Xóa vĩnh viễn các người dùng đã được xóa mềm.
    /// </summary>
    Task PurgeSoftDeletedUsersAsync();
    Task PurgeOldAuditLogsAsync();

    Task PurgeRevokedRefreshTokensAsync();

    /// <summary>
    /// Thực hiện xóa vĩnh viễn một nhóm và toàn bộ dữ liệu liên quan.
    /// </summary>
    [DisplayName("Permanently Delete Group and all related data for GroupId: {0}")]
    Task PermanentlyDeleteGroupAsync(Guid groupId);

    [DisplayName("Clean up orphaned files intended for post attachments")]
    Task CleanUpOrphanedPostFilesAsync();

    [DisplayName("Permanently delete old recalled messages")]
    Task PermanentlyDeleteOldRecalledMessagesAsync();

    [DisplayName("Delete old login history records")]
    Task DeleteOldLoginHistoryAsync();
    [DisplayName("Permanently Delete Conversation ID: {0}")]
    Task PermanentlyDeleteConversationAsync(int conversationId);
}
