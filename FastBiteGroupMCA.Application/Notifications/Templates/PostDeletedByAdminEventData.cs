using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record PostDeletedByAdminEventData(Posts Post, AppUser DeletedByUser);

public class PostDeletedByAdminNotificationTemplate : INotificationTemplate<PostDeletedByAdminEventData>
{
    // Có thể cần thêm EnumNotificationType.PostDeleted
    public EnumNotificationType NotificationType => EnumNotificationType.SystemAnnouncement;

    public string BuildContent(PostDeletedByAdminEventData data)
        => $"Bài viết '{data.Post.Title ?? "không có tiêu đề"}' của bạn trong nhóm '{data.Post.Group.GroupName}' đã bị xóa bởi {data.DeletedByUser.FullName}.";

    public RelatedObjectInfo BuildRelatedObject(PostDeletedByAdminEventData data)
    {
        // Link đến nhóm vì bài viết đã bị xóa
        return new()
        {
            ObjectType = EnumNotificationObjectType.Group,
            ObjectId = data.Post.GroupID.ToString(),
            NavigateUrl = $"/groups/{data.Post.GroupID}"
        };
    }
}
