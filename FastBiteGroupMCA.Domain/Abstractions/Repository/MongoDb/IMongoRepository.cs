using MongoDB.Driver;
using System.Linq.Expressions;

namespace FastBiteGroupMCA.Domain.Abstractions.Repository.MongoDb;

public interface IMongoRepository<TDocument> where TDocument : IDocument
{
    Task<TDocument> GetByIdAsync(string id);
    Task InsertOneAsync(TDocument document);
    Task InsertManyAsync(List<TDocument> documents);
    Task ReplaceOneAsync(TDocument document);
    Task<bool> UpdateOneAsync(FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> update);
    Task DeleteByIdAsync(string id);
    Task<List<TDocument>> GetAllAsync();
    IQueryable<TDocument> GetQueryable();
    /// <summary>
    /// Đếm số lượng document khớp với một điều kiện lọc.
    /// </summary>
    Task<long> CountAsync(Expression<Func<TDocument, bool>> filterExpression);

    /// <summary>
    /// Lấy danh sách document có phân trang, lọc và sắp xếp.
    /// </summary>
    Task<List<TDocument>> GetPagedAsync(
        Expression<Func<TDocument, bool>> filterExpression,
        int pageNumber,
        int pageSize,
        Expression<Func<TDocument, object>> sortExpression,
        bool isDescending = true);
}
