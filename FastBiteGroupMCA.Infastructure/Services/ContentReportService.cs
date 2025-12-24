using FastBiteGroupMCA.Application.DTOs.ContentReport;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.Notifications.Templates;
using Hangfire;
using System.ComponentModel;

namespace FastBiteGroupMCA.Infastructure.Services;

public class ContentReportService : IContentReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ISettingsService _settingsService;
    private readonly IUserService _userService;
    private readonly ILogger<ContentReportService> _logger;
    private readonly IAdminNotificationService _adminNotificationService;

    public ContentReportService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ContentReportService> logger,
        IAdminNotificationService adminNotificationService,
        ISettingsService settingsService,
        IUserService userService,
        IBackgroundJobClient backgroundJobClient,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
        _adminNotificationService = adminNotificationService;
        _settingsService = settingsService;
        _userService = userService;
        _backgroundJobClient = backgroundJobClient;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<object>> ReportContentAsync(Guid groupId, CreateContentReportDto dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var validationData = await _unitOfWork.Groups.GetQueryable()
            .Where(g => g.GroupID == groupId && !g.IsDeleted)
            .Select(g => new
            {
                GroupExists = true,
                IsMember = g.Members.Any(m => m.UserID == userId),
                ContentExists = dto.ContentType == EnumReportedContentType.Post
                    ? g.Posts.Any(p => p.PostID == dto.ContentId && !p.IsDeleted)
                    : g.Posts.SelectMany(p => p.Comments).Any(c => c.CommentID == dto.ContentId && !c.IsDeleted),
                AlreadyReported = g.ContentReports.Any(r => r.ReportedByUserID == userId && r.ReportedContentID == dto.ContentId && r.ReportedContentType == dto.ContentType)
            })
            .FirstOrDefaultAsync();

        if (validationData == null || !validationData.GroupExists)
            return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.", 404);

        if (!validationData.IsMember)
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không phải là thành viên của nhóm này.", 403);

        if (!validationData.ContentExists)
            return ApiResponse<object>.Fail("CONTENT_NOT_FOUND", "Nội dung bạn báo cáo không tồn tại trong nhóm này.", 404);

        if (validationData.AlreadyReported)
            return ApiResponse<object>.Fail("ALREADY_REPORTED", "Bạn đã báo cáo nội dung này trước đó.", 409);

        Guid? reportedUserId = null;
        if (dto.ContentType == EnumReportedContentType.Post)
        {
            reportedUserId = await _unitOfWork.Posts.GetQueryable()
                .Where(p => p.PostID == dto.ContentId)
                .Select(p => (Guid?)p.AuthorUserID)
                .FirstOrDefaultAsync();
        }
        else // Comment
        {
            reportedUserId = await _unitOfWork.PostComments.GetQueryable()
                .Where(c => c.CommentID == dto.ContentId)
                .Select(c => (Guid?)c.UserID)
                .FirstOrDefaultAsync();
        }

        if (reportedUserId == null)
        {
            return ApiResponse<object>.Fail("CONTENT_OWNER_NOT_FOUND", "Không tìm thấy tác giả của nội dung này.");
        }

        var report = new ContentReport
        {
            ReportedContentID = dto.ContentId,
            ReportedContentType = dto.ContentType,
            Reason = dto.Reason,
            ReportedByUserID = userId,
            GroupID = groupId,
            Status = EnumContentReportStatus.Pending,
        };

        await _unitOfWork.ContentReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue(() =>
             CheckForAutoLockAsync(reportedUserId.Value));

        // Gửi thông báo cho Admin/Mod (không đổi)
        _backgroundJobClient.Enqueue(() =>
            NotifyGroupAdminsOfNewReportAsync(groupId, report.ContentReportID, userId));

        return ApiResponse<object>.Created(null, "Báo cáo của bạn đã được gửi đến quản trị viên nhóm để xem xét.");
    }

    public async Task CheckForAutoLockAsync(Guid reportedUserId)
    {
        var autoLockThreshold = _settingsService.Get<int>(SettingKeys.AutoLockAccountThreshold, 0);

        if (autoLockThreshold > 0)
        {
            var reportCount = await _unitOfWork.ContentReports.GetQueryable()
                .CountAsync(r => r.ReportedContentOwnerId == reportedUserId &&
                                 r.Status == EnumContentReportStatus.Pending &&
                                 r.CreatedAt > DateTime.UtcNow.AddDays(-30));

            if (reportCount >= autoLockThreshold)
            {
                _logger.LogInformation("User {UserId} has reached the auto-lock threshold of {Threshold}. Deactivating account.", reportedUserId, autoLockThreshold);
                await _userService.DeactivateUserAccountAsync(reportedUserId, "Tài khoản bị tạm khóa do có nhiều báo cáo vi phạm.");
            }
        }
    }

    // Phương thức public mới để Hangfire có thể gọi
    [DisplayName("Notify Group Admins of New Report: {0}")]
    public async Task NotifyGroupAdminsOfNewReportAsync(Guid groupId, int reportId, Guid reporterId)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        var report = await _unitOfWork.ContentReports.GetByIdAsync(reportId);
        var reporter = await _unitOfWork.Users.GetByIdAsync(reporterId);

        if (group == null || report == null || reporter == null) return;

        var adminAndModIds = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(m => m.GroupID == groupId && (m.Role == EnumGroupRole.Admin || m.Role == EnumGroupRole.Moderator))
            .Select(m => m.UserID)
            .ToListAsync();

        var eventData = new NewContentReportEventData(group, report, reporter);

        foreach (var adminId in adminAndModIds)
        {
            await _notificationService.DispatchNotificationAsync<NewContentReportNotificationTemplate, NewContentReportEventData>(adminId, eventData);
        }
    }
}
