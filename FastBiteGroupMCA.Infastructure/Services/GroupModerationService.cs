using FastBiteGroupMCA.Application.DTOs.ContentReport;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Notifications.Templates;
using Hangfire;
using System.ComponentModel;

namespace FastBiteGroupMCA.Infastructure.Services;

public class GroupModerationService : IGroupModerationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IGroupService _groupService;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<GroupModerationService> _logger;

    public GroupModerationService(
        IUnitOfWork unitOfWork, 
        ILogger<GroupModerationService> logger, 
        ICurrentUser currentUser, 
        IBackgroundJobClient backgroundJobClient, 
        IGroupService groupService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUser = currentUser;
        _backgroundJobClient = backgroundJobClient;
        _groupService = groupService;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<PagedResult<GroupReportedContentDto>>> GetPendingReportsAsync(Guid groupId, GetPendingReportsQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PagedResult<GroupReportedContentDto>>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var member = await _unitOfWork.GroupMembers.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.GroupID == groupId && m.UserID == userId);

        if (member == null || (member.Role != EnumGroupRole.Admin && member.Role != EnumGroupRole.Moderator))
        {
            return ApiResponse<PagedResult<GroupReportedContentDto>>.Fail("FORBIDDEN", "Bạn không có quyền truy cập mục kiểm duyệt của nhóm này.", 403);
        }

        try
        {
            var reportsQuery = _unitOfWork.ContentReports.GetQueryable()
             .Where(r => r.GroupID == groupId && r.Status == EnumContentReportStatus.Pending);

            if (query.ContentType.HasValue)
            {
                reportsQuery = reportsQuery.Where(r => r.ReportedContentType == query.ContentType.Value);
            }
            if (query.ReporterId.HasValue)
            {
                reportsQuery = reportsQuery.Where(r => r.ReportedByUserID == query.ReporterId.Value);
            }
            if (query.AuthorId.HasValue)
            {
                reportsQuery = reportsQuery.Where(r => r.ReportedContentOwnerId == query.AuthorId.Value);
            }

            var orderedQuery = query.SortBy?.ToLower() switch
            {
                "oldest" => reportsQuery.OrderBy(r => r.CreatedAt),
                _ => reportsQuery.OrderByDescending(r => r.CreatedAt) // Mặc định là "newest"
            };

            var pagedReports = await reportsQuery
            .OrderByDescending(r => r.CreatedAt)
            .ToPagedResultAsync(query.PageNumber, query.PageSize);

            var reports = pagedReports.Items;
            if (!reports.Any())
            {
                return ApiResponse<PagedResult<GroupReportedContentDto>>.Ok(new PagedResult<GroupReportedContentDto>(new List<GroupReportedContentDto>(), 0, query.PageNumber, query.PageSize));
            }

            // BƯỚC 3: "Gom" các ID cần thiết từ danh sách báo cáo đã phân trang
            var postIds = reports.Where(r => r.ReportedContentType == EnumReportedContentType.Post).Select(r => r.ReportedContentID).ToList();
            var commentIds = reports.Where(r => r.ReportedContentType == EnumReportedContentType.Comment).Select(r => r.ReportedContentID).ToList();
            var userIds = reports.Select(r => r.ReportedByUserID)
                                 .Union(reports.Select(r => r.ReportedContentOwnerId))
                                 .ToHashSet();

            // BƯỚC 4: Truy vấn các thông tin chi tiết trong các lượt gọi riêng biệt
            var posts = postIds.Any() ? await _unitOfWork.Posts.GetQueryable()
                .Include(p => p.Author).Where(p => postIds.Contains(p.PostID)).ToListAsync() : new List<Posts>();

            var comments = commentIds.Any() ? await _unitOfWork.PostComments.GetQueryable()
                .Include(c => c.User).Where(c => commentIds.Contains(c.CommentID)).ToListAsync() : new List<PostComments>();

            var users = userIds.Any() ? await _unitOfWork.Users.GetQueryable()
                .Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id) : new Dictionary<Guid, AppUser>();

            // BƯỚC 5: Map kết quả sang DTO trong bộ nhớ
            var resultItems = reports.Select(r =>
            {
                string contentPreview = "[Nội dung đã bị xóa]";
                string authorName = "[Không rõ]";

                if (r.ReportedContentType == EnumReportedContentType.Post)
                {
                    var post = posts.FirstOrDefault(p => p.PostID == r.ReportedContentID);
                    if (post != null)
                    {
                        contentPreview = post.Title ?? (post.ContentJson.Length > 100 ? post.ContentJson[..100] + "..." : post.ContentJson);
                        authorName = post.Author?.FullName ?? "[Không rõ]";
                    }
                }
                else // Comment
                {
                    var comment = comments.FirstOrDefault(c => c.CommentID == r.ReportedContentID);
                    if (comment != null)
                    {
                        contentPreview = comment.Content.Length > 100 ? comment.Content[..100] + "..." : comment.Content;
                        authorName = comment.User?.FullName ?? "[Không rõ]";
                    }
                }

                var reporterName = users.TryGetValue(r.ReportedByUserID, out var reporter) ? reporter.FullName : "[Không rõ]";

                return new GroupReportedContentDto
                {
                    ReportId = r.ContentReportID,
                    ContentId = r.ReportedContentID,
                    ContentType = r.ReportedContentType.ToString(),
                    ContentPreview = contentPreview,
                    AuthorName = authorName,
                    ReporterName = reporterName ?? "[Không rõ]",
                    Reason = r.Reason,
                    ReportedAt = r.CreatedAt
                };
            }).ToList();

            var finalPagedResult = new PagedResult<GroupReportedContentDto>(resultItems, pagedReports.TotalRecords, query.PageNumber, query.PageSize);
            return ApiResponse<PagedResult<GroupReportedContentDto>>.Ok(finalPagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách báo cáo cho nhóm {GroupId}", groupId);
            return ApiResponse<PagedResult<GroupReportedContentDto>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }
    public async Task<ApiResponse<object>> TakeModerationActionAsync(Guid groupId, int reportId, Guid moderatorId, ModerationActionDto dto)
    {
        var member = await _unitOfWork.GroupMembers.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.GroupID == groupId && m.UserID == moderatorId);

        if (member == null || (member.Role != EnumGroupRole.Admin && member.Role != EnumGroupRole.Moderator))
        {
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền truy cập mục kiểm duyệt của nhóm này.", 403);
        }

        var report = await _unitOfWork.ContentReports
            .GetQueryable()
            .FirstOrDefaultAsync(r => r.ContentReportID == reportId && r.GroupID == groupId && r.Status == EnumContentReportStatus.Pending);

        if (report == null)
        {
            return ApiResponse<object>.Fail("NOT_FOUND", "Báo cáo không tồn tại hoặc đã được xử lý.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            bool contentWasRemoved = false;
            Guid reportedUserId = report.ReportedContentOwnerId;

            switch (dto.Action)
            {
                case EnumModerationAction.DismissReport:
                    report.Status = EnumContentReportStatus.Rejected;
                    break;

                case EnumModerationAction.RemoveContent:
                case EnumModerationAction.RemoveContentAndWarnUser:
                case EnumModerationAction.RemoveContentAndBanUser:
                    await SoftDeleteContent(report.ReportedContentType, report.ReportedContentID); // Dùng phiên bản đã sửa lỗi
                    report.Status = EnumContentReportStatus.Reviewed;
                    contentWasRemoved = true;
                    break;

                default: return ApiResponse<object>.Fail("INVALID_ACTION", "Hành động không hợp lệ.");
            }

            // CẢI TIẾN: Xử lý việc Ban User ngay trong transaction này
            if (dto.Action == EnumModerationAction.RemoveContentAndBanUser)
            {
                var memberToKick = await _unitOfWork.GroupMembers.GetQueryable()
                    .FirstOrDefaultAsync(m => m.GroupID == groupId && m.UserID == reportedUserId);
                if (memberToKick != null)
                {
                    // Logic cấp thấp của việc kick: Xóa khỏi GroupMembers và ConversationParticipants
                    _unitOfWork.GroupMembers.Remove(memberToKick);
                    // ... thêm logic xóa khỏi ConversationParticipants nếu cần ...
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            // Gửi các thông báo qua tác vụ nền
            _backgroundJobClient.Enqueue(() =>
                NotifyUsersAfterModerationAsync(reportId, moderatorId, dto.Action, contentWasRemoved));

            _logger.LogInformation("User {UserId} đã xử lý báo cáo {ReportId} với hành động {Action}", moderatorId, reportId, dto.Action);
            return ApiResponse<object>.Ok(null, "Hành động đã được thực hiện thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý báo cáo {ReportId}", reportId);
            return ApiResponse<object>.Fail("INTERNAL_ERROR", "Đã xảy ra lỗi khi xử lý hành động.");
        }
    }

    private async Task SoftDeleteContent(EnumReportedContentType contentType, int contentId)
    {
        if (contentType == EnumReportedContentType.Post)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(contentId);
            if (post != null) post.IsDeleted = true;
        }
        else if (contentType == EnumReportedContentType.Comment)
        {
            var comment = await _unitOfWork.PostComments.GetByIdAsync(contentId);
            if (comment != null)
            {
                comment.IsDeleted = true;
            }
        }
    }

    [DisplayName("Notify Users After Moderation for Report: {0}")]
    public async Task NotifyUsersAfterModerationAsync(int reportId, Guid moderatorId, EnumModerationAction action, bool wasContentRemoved)
    {
        var report = await _unitOfWork.ContentReports.GetQueryable()
            .Include(r => r.Group)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ContentReportID == reportId);
        if (report == null) return;

        // 1. Gửi thông báo cho người báo cáo
        var feedbackEventData = new ReportFeedbackEventData(report, report.Group);
        await _notificationService.DispatchNotificationAsync<ReportFeedbackNotificationTemplate, ReportFeedbackEventData>(
            report.ReportedByUserID,
            feedbackEventData);

        // 2. Gửi thông báo cho người bị báo cáo (nếu nội dung của họ bị xóa)
        if (wasContentRemoved)
        {
            var actionTakenEventData = new ActionTakenOnContentEventData(report, report.Group, action);
            await _notificationService.DispatchNotificationAsync<ActionTakenOnContentNotificationTemplate, ActionTakenOnContentEventData>(
                report.ReportedContentOwnerId,
                actionTakenEventData);
        }
    }
}
