using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates
{
    public record PostLikeEventData(Posts Post, AppUser Liker);
    public class PostLikeNotificationTemplate : INotificationTemplate<PostLikeEventData>
    {
        public EnumNotificationType NotificationType => EnumNotificationType.PostLike;

        public string BuildContent(PostLikeEventData data)
            => $"{data.Liker.FullName} đã thích bài viết của bạn.";

        public RelatedObjectInfo BuildRelatedObject(PostLikeEventData data)
            => new()
            {
                ObjectType = EnumNotificationObjectType.Post,
                ObjectId = data.Post.PostID.ToString(),
                NavigateUrl = $"/groups/{data.Post.GroupID}/posts/{data.Post.PostID}"
            };
    }
}
