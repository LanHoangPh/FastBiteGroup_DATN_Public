using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FastBiteGroupMCA.Domain.Entities;

public class SenderInfo
{
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }
}
