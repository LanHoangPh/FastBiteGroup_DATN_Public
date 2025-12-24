using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Notification;
using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.IServices;

public interface INotificationService
{

    ///// <summary>
    ///// Hàm tiện ích: Tạo, lưu và gửi thông báo cho người dùng cuối.
    ///// Tự động chọn kênh gửi (SignalR/OneSignal) dựa trên trạng thái online.
    ///// </summary>
    ///// <param name="targetUserId">ID người dùng nhận thông báo.</param>
    ///// <param name="notificationType">Loại thông báo.</param>
    ///// <param name="contentPreview">Nội dung xem trước (dùng cho push notification và hiển thị tóm tắt).</param>
    ///// <param name="relatedObject">Đối tượng liên quan (bài post, tin nhắn...) để điều hướng.</param>
    //Task CreateAndSendNotificationAsync(
    //    Guid targetUserId,
    //    EnumNotificationType notificationType,
    //    string contentPreview,
    //    RelatedObjectInfo? relatedObject = null);
    /// <summary>
    /// Hàm tiện ích nâng cao: Tìm và thực thi một template thông báo cụ thể.
    /// </summary>
    Task DispatchNotificationAsync<TTemplate, TEventData>(Guid targetUserId, TEventData eventData)
        where TTemplate : class, INotificationTemplate<TEventData>;
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<ApiResponse<PagedResult<NotificationDTO>>> GetMyNotificationsAsync(Guid userId, PaginationParams pageParams);
    Task<ApiResponse<object>> MarkAsReadAsync(string notificationId, Guid userId);
    Task<ApiResponse<object>> MarkAllAsReadAsync(Guid userId);
    /// <summary>
    /// Gửi thông báo khi Admin tổng thêm một người dùng vào nhóm.
    /// </summary>
    Task NotifyUserAddedToGroupByAdminAsync(
        Guid addedUserId,
        string addedUserName,
        Guid groupId,
        string groupName,
        string systemAdminName);
}
