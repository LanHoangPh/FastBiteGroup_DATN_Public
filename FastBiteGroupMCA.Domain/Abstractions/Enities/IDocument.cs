using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace FastBiteGroupMCA.Domain.Abstractions.Enities;

public interface IDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    string Id { get; set; }
}
