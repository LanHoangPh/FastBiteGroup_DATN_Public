using FastBiteGroupMCA.Domain.Abstractions;
using FastBiteGroupMCA.Domain.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FastBiteGroupMCA.Domain.Entities;
[BsonCollection("notifications")]
public class Notifications : BaseDocument
{
    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; } // Người nhận thông báo (UserID từ SQL)

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public EnumNotificationType Type { get; set; } // "PostLike", "UserMention", "NewComment"...

    [BsonElement("content_preview")]
    public string ContentPreview { get; set; } = string.Empty; // Ví dụ: "Bình đã thích bài đăng của bạn"

    [BsonElement("is_read")]
    public bool IsRead { get; set; }

    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    // Thông tin về đối tượng liên quan, thay thế cho bảng NotificationObjectLinks
    [BsonElement("related_object")]
    public RelatedObjectInfo? RelatedObject { get; set; }
}
