using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using FastBiteGroupMCA.Domain.Attributes;

namespace FastBiteGroupMCA.Domain.Entities;

// Thông tin về một reaction, thay thế bảng MessageReactions
[BsonCollection("reactions")]
public class Reaction
{
    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; } // UserID từ SQL

    [BsonElement("code")]
    public string ReactionCode { get; set; } = string.Empty; // Ví dụ: ":thumbs_up:"

    [BsonElement("reacted_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReactedAt { get; set; }
}
