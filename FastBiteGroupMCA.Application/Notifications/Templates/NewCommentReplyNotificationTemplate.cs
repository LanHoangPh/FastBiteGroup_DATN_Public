using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

/// <summary>
/// Dữ liệu cần thiết để tạo thông báo có người trả lời bình luận.
/// </summary>
public record NewCommentReplyEventData(
    Posts Post,
    PostComments OriginalComment, // Bình luận gốc
    PostComments ReplyComment,      // Bình luận trả lời
    AppUser Replier                 // Người trả lời
);

/// <summary>
/// Template để xây dựng thông báo.
/// </summary>
public class NewCommentReplyNotificationTemplate : INotificationTemplate<NewCommentReplyEventData>
{
    // Có thể tạo một EnumNotificationType mới là "NewCommentReply"
    public EnumNotificationType NotificationType => EnumNotificationType.NewPostComment;

    public string BuildContent(NewCommentReplyEventData data)
    {
        // Trích một đoạn ngắn của bình luận gốc
        var originalCommentSnippet = data.OriginalComment.Content.Length > 50
            ? data.OriginalComment.Content.Substring(0, 50) + "..."
            : data.OriginalComment.Content;

        return $"<strong>{data.Replier.FullName}</strong> đã trả lời bình luận của bạn \"{originalCommentSnippet}\" trong bài viết <strong>{data.Post.Title ?? "một bài viết"}</strong>.";
    }

    public RelatedObjectInfo BuildRelatedObject(NewCommentReplyEventData data)
    {
        // Điều hướng thẳng đến bài viết và có thể highlight bình luận trả lời
        return new()
        {
            ObjectType = EnumNotificationObjectType.Post,
            ObjectId = data.Post.PostID.ToString(),
            NavigateUrl = $"/groups/{data.Post.GroupID}/posts/{data.Post.PostID}?replyId={data.ReplyComment.CommentID}"
        };
    }
}