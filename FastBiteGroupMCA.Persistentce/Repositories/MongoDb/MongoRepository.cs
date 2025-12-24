using FastBiteGroupMCA.Domain.Abstractions.Enities;
using FastBiteGroupMCA.Domain.Abstractions.Repository.MongoDb;
using FastBiteGroupMCA.Domain.Attributes;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Reflection;

namespace FastBiteGroupMCA.Persistentce.Repositories.MongoDb;

public class MongoRepository<TDocument> : IMongoRepository<TDocument> where TDocument : IDocument
{
    protected readonly IMongoCollection<TDocument> _collection;
    public MongoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
    }
    private string GetCollectionName(Type documentType)
    {
        return (documentType.GetCustomAttribute(typeof(BsonCollectionAttribute), true) as BsonCollectionAttribute)?.CollectionName ?? (documentType.Name.ToLower());
    }


    public async Task DeleteByIdAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        await _collection.DeleteOneAsync(filter);
    }

    public async Task<List<TDocument>> GetAllAsync()
    {
        return await _collection.Find(Builders<TDocument>.Filter.Empty).ToListAsync();
    }

    public async Task<TDocument> GetByIdAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task InsertOneAsync(TDocument document)
    {
        await _collection.InsertOneAsync(document);
    }

    public async Task ReplaceOneAsync(TDocument document)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
        await _collection.ReplaceOneAsync(filter, document);
    }

    public IQueryable<TDocument> GetQueryable()
    {
        return _collection.AsQueryable();
    }

    public Task InsertManyAsync(List<TDocument> documents)
    {
        return _collection.InsertManyAsync(documents);
    }

    public Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression)
    {
        return _collection.CountDocumentsAsync(filterExpression);
    }

    public async Task<List<TDocument>> GetPagedAsync(Expression<Func<TDocument, bool>> filterExpression, int pageNumber, int pageSize, Expression<Func<TDocument, object>> sortExpression, bool isDescending = true)
    {
        var findFluent = _collection.Find(filterExpression);

        var sortDefinition = isDescending
            ? Builders<TDocument>.Sort.Descending(sortExpression)
            : Builders<TDocument>.Sort.Ascending(sortExpression);

        return await findFluent
            .Sort(sortDefinition)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<bool> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update)
    {
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }
}