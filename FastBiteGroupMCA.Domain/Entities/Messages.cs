using FastBiteGroupMCA.Domain.Abstractions;
using FastBiteGroupMCA.Domain.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FastBiteGroupMCA.Domain.Entities;
[BsonCollection("messages")]
public class Messages : BaseDocument
{
    [BsonElement("conversation_id")]
    public int ConversationId { get; set; }

    [BsonElement("sender")]
    public SenderInfo? Sender { get; set; } // user a 

    [BsonElement("content")]
    [BsonIgnoreIfNull] 
    public string Content { get; set; } = string.Empty; 

    [BsonElement("message_type")]
    [BsonRepresentation(BsonType.String)]
    public EnumMessageType MessageType { get; set; } = EnumMessageType.Text; // "Text", "Image", "File", "Poll"... 

    [BsonElement("sent_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SentAt { get; set; }

    [BsonElement("parent_message_id")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentMessageId { get; set; }
    [BsonElement("is_deleted")]
    public bool IsDeleted { get; set; } = false;
    /// <summary>
    /// Dành cho tính năng thu h hồi tin nhắn, đánh dấu thời gian khi tin nhắn bị xóa.
    /// </summary>
    [BsonElement("deleted_at")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)] 
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Bản xem trước của tin nhắn gốc (dành cho tính năng trả lời).
    /// </summary>
    [BsonElement("parent_message")]
    [BsonIgnoreIfNull]
    public ParentMessageInfo? ParentMessage { get; set; }
    [BsonElement("attachments")]
    [BsonIgnoreIfNull]
    public List<AttachmentInfo>? Attachments { get; set; }

    [BsonElement("reactions")]
    [BsonIgnoreIfNull]
    public List<Reaction>? Reactions { get; set; }

    [BsonElement("mentions")]
    [BsonIgnoreIfNull]
    public List<Guid>? MentionedUserIds { get; set; }  

    [BsonElement("read_by")]
    [BsonIgnoreIfNull]
    public List<ReadReceiptInfo>? ReadBy { get; set; }
    
}
