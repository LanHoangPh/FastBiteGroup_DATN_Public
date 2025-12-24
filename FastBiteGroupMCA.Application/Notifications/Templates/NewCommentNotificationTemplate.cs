using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record NewCommentEventData(int PostId, int CommentId, Guid CommenterId);

public class NewCommentNotificationTemplate : INotificationTemplate<NewCommentEventData>
{
    // 2. Inject IUnitOfWork để template có thể tự lấy dữ liệu
    private readonly IUnitOfWork _unitOfWork;
    public NewCommentNotificationTemplate(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public EnumNotificationType NotificationType => EnumNotificationType.NewPostComment;

    // 3. BuildContent giờ đây sẽ tự lấy dữ liệu từ DB
    public string BuildContent(NewCommentEventData data)
    {
        // Dùng .Result ở đây là chấp nhận được vì nó chạy trong background job
        var post = _unitOfWork.Posts.GetByIdAsync(data.PostId).Result;
        var commenter = _unitOfWork.Users.GetByIdAsync(data.CommenterId).Result;

        if (post == null || commenter == null)
            return "Có một bình luận mới trong một bài viết bạn theo dõi.";

        return $"<strong>{commenter.FullName}</strong> đã bình luận về bài viết của bạn: <strong>{post.Title ?? "một bài viết"}</strong>.";
    }

    public RelatedObjectInfo BuildRelatedObject(NewCommentEventData data)
    {
        var post = _unitOfWork.Posts.GetByIdAsync(data.PostId).Result;
        if (post == null) return null;

        return new()
        {
            ObjectType = EnumNotificationObjectType.Post,
            ObjectId = post.PostID.ToString(),
            NavigateUrl = $"/groups/{post.GroupID}/posts/{post.PostID}?commentId={data.CommentId}"
        };
    }
}
