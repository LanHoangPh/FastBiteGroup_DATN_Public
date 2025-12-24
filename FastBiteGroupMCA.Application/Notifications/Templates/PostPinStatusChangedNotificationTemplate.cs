using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record PostPinStatusChangedEventData(Posts Post, AppUser ChangedByUser, bool IsPinned);

public class PostPinStatusChangedNotificationTemplate : INotificationTemplate<PostPinStatusChangedEventData>
{
    // Có thể cần thêm EnumNotificationType.PostPinned
    public EnumNotificationType NotificationType => EnumNotificationType.SystemAnnouncement;

    public string BuildContent(PostPinStatusChangedEventData data)
    {
        var actionText = data.IsPinned ? "ghim" : "bỏ ghim";
        return $"{data.ChangedByUser.FullName} đã {actionText} bài viết của bạn: '{data.Post.Title ?? "một bài viết"}'.";
    }

    public RelatedObjectInfo BuildRelatedObject(PostPinStatusChangedEventData data)
    {
        // Link thẳng đến bài viết
        return new()
        {
            ObjectType = EnumNotificationObjectType.Post,
            ObjectId = data.Post.PostID.ToString(),
            NavigateUrl = $"/groups/{data.Post.GroupID}/posts/{data.Post.PostID}"
        };
    }
}
