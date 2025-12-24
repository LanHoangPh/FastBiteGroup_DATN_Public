namespace FastBiteGroupMCA.Domain.Attributes;
/// <summary>
/// Attribute to specify the MongoDB collection name for a document class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }
    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}
