using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using FastBiteGroupMCA.Domain.Attributes;

namespace FastBiteGroupMCA.Domain.Entities;
[BsonCollection("related_objects")]
public class RelatedObjectInfo
{
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public EnumNotificationObjectType ObjectType { get; set; }  // "Post", "Message", "Group"

    // ID có thể là int (PostID) hoặc string (MessageID)
    // Dùng string để linh hoạt
    [BsonElement("id")]
    public string? ObjectId { get; set; } // ID của đối tượng liên quan, có thể là PostID, MessageID hoặc GroupID

    [BsonElement("url")]
    public string NavigateUrl { get; set; } = string.Empty; // URL để điều hướng khi click
}
