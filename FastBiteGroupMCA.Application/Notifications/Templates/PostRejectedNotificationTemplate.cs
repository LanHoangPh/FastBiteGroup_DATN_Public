using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

/// <summary>
/// Dữ liệu cần thiết để tạo thông báo bài viết bị từ chối.
/// </summary>
public record PostRejectedEventData(Posts Post, string RejectionReason);

/// <summary>
/// Template để xây dựng thông báo khi bài viết của người dùng bị từ chối.
/// </summary>
public class PostRejectedNotificationTemplate : INotificationTemplate<PostRejectedEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.PostRejected;

    public string BuildContent(PostRejectedEventData data)
    {
        // Trích xuất một đoạn ngắn của nội dung để người dùng nhận biết
        var contentSnippet = data.Post.Title ?? (data.Post.ContentJson.Length > 50
            ? data.Post.ContentJson.Substring(0, 50) + "..."
            : data.Post.ContentJson);

        return $"Bài viết '{contentSnippet}' của bạn đã không được duyệt vì: {data.RejectionReason}";
    }

    public RelatedObjectInfo BuildRelatedObject(PostRejectedEventData data)
    {
        // Không có link điều hướng vì bài viết đã bị từ chối và có thể không xem được.
        return null!;
    }
}
