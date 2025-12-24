using MongoDB.Bson;

namespace FastBiteGroupMCA.Domain.Abstractions;

public abstract class BaseDocument : IDocument
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
}
