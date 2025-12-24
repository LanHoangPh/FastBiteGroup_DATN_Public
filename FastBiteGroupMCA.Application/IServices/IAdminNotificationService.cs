using FastBiteGroupMCA.Application.DTOs.Admin.AdminNotifications;
using FastBiteGroupMCA.Application.DTOs.Notification;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.IServices
{
    public interface IAdminNotificationService
    {
        Task<ApiResponse<bool>> EnqueueBroadcastAsync(CreateAnnouncementRequestDTO requestDto);
        Task BroadcastAsync(CreateAnnouncementRequestDTO dto);
        /// <summary>
        /// Tạo và broadcast một thông báo mới cho Admin.
        /// </summary>
        Task CreateAndBroadcastNotificationAsync(
            EnumAdminNotificationType type,
            string message,
            string? linkTo = null,
            Guid? triggeredByUserId = null
        );

        Task SendBulkActionCompletionNotificationAsync(Guid adminId, Guid batchId, int totalJobs, string actionType);
        Task SendExportReadyNotificationAsync(Guid adminId, string fileName, string fileUrl);
        Task SendExportFailedNotificationAsync(Guid adminId, string fileName, string details);

        /// <summary>
        /// Lấy danh sách thông báo cho Admin (phân trang).
        /// </summary>
        Task<ApiResponse<PagedResult<AdminNotificationDto>>> GetNotificationsAsync(GetAdminNotificationsParams request);

        /// <summary>
        /// Đánh dấu một thông báo cụ thể là đã đọc.
        /// </summary>
        Task<ApiResponse<object>> MarkAsReadAsync(long notificationId);

        /// <summary>
        /// Đánh dấu tất cả thông báo chưa đọc là đã đọc.
        /// </summary>
        Task<ApiResponse<object>> MarkAllAsReadAsync();
    }
}
