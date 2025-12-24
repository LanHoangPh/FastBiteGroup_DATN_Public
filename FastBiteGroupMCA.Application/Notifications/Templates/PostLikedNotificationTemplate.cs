using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

/// <summary>
/// Dữ liệu cần thiết để tạo thông báo có người thích bài viết.
/// </summary>
public record PostLikedEventData(int PostId, Guid LikerId);

/// <summary>
/// Template để xây dựng thông báo.
/// </summary>
public class PostLikedNotificationTemplate : INotificationTemplate<PostLikedEventData>
{
    private readonly IUnitOfWork _unitOfWork;
    public PostLikedNotificationTemplate(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }

    public EnumNotificationType NotificationType => EnumNotificationType.PostLike;

    // BuildContent giờ đây sẽ tự lấy dữ liệu
    public string BuildContent(PostLikedEventData data)
    {
        var post = _unitOfWork.Posts.GetByIdAsync(data.PostId).Result;
        var liker = _unitOfWork.Users.GetByIdAsync(data.LikerId).Result;

        if (post == null || liker == null) return "Có người đã thích bài viết của bạn.";

        return $"<strong>{liker.FullName}</strong> đã thích bài viết của bạn: <strong>{post.Title ?? "một bài viết"}</strong>.";
    }

    public RelatedObjectInfo BuildRelatedObject(PostLikedEventData data)
    {
        // Điều hướng người dùng thẳng đến bài viết đã được thích
        return new()
        {
            ObjectType = EnumNotificationObjectType.Post,
            ObjectId = data.PostId.ToString(),
            NavigateUrl = $"/groups/{data.PostId}/posts/{data}"
        };
    }
}
