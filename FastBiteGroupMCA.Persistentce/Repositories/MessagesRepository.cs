using FastBiteGroupMCA.Domain.Abstractions;
using FastBiteGroupMCA.Domain.Enum;
using FastBiteGroupMCA.Persistentce.Repositories.MongoDb;
using MongoDB.Driver;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class    MessagesRepository : MongoRepository<Messages>, IMessagesRepository
{
    public MessagesRepository(IMongoDatabase database) : base(database)
    {
    }
    public async Task<List<Messages>> GetMessagesBeforeAsync(int conversationId, string? beforeMessageId, int limit)
    {
        var filterBuilder = Builders<Messages>.Filter;
        var filter = filterBuilder.Eq(m => m.ConversationId, conversationId);

        if (!string.IsNullOrEmpty(beforeMessageId))
        {
            filter &= filterBuilder.Lt(m => m.Id, beforeMessageId);
        }

        var findOptions = new FindOptions<Messages, Messages>
        {
            Sort = Builders<Messages>.Sort.Descending(m => m.Id),
            Limit = limit
        };

        var cursor = await _collection.FindAsync(filter, findOptions);
        return await cursor.ToListAsync();
    }

    public async Task<bool> HasOlderMessagesAsync(int conversationId, string oldestMessageIdInBatch)
    {
        var filter = Builders<Messages>.Filter.And(
            Builders<Messages>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<Messages>.Filter.Lt(m => m.Id, oldestMessageIdInBatch)
        );
        return await _collection.Find(filter).Limit(1).AnyAsync();
    }

    public async Task<long> MarkMessagesAsReadAsync(IEnumerable<string> messageIds, ReadReceiptInfo readerInfo)
    {
        var filter = Builders<Messages>.Filter.In(m => m.Id, messageIds);

        var pullUpdate = Builders<Messages>.Update.PullFilter(m => m.ReadBy,
            r => r.UserId == readerInfo.UserId);

        await _collection.UpdateManyAsync(filter, pullUpdate);

        var pushUpdate = Builders<Messages>.Update.Push(m => m.ReadBy, readerInfo);

        var result = await _collection.UpdateManyAsync(filter, pushUpdate);

        return result.ModifiedCount;
    }
    public async Task<Messages> FindOneAndUpdateAsync(FilterDefinition<Messages> filter, UpdateDefinition<Messages> update, bool returnNewDocument = true)
    {
        var options = new FindOneAndUpdateOptions<Messages>
        {
            ReturnDocument = returnNewDocument ? ReturnDocument.After : ReturnDocument.Before
        };
        return await _collection.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task<Dictionary<int, Messages>> GetLastMessageForConversationsAsync(IEnumerable<int> conversationIds)
    {
        if (conversationIds == null || !conversationIds.Any())
        {
            return new Dictionary<int, Messages>();
        }

        var aggregation = _collection.Aggregate()
            .Match(m => conversationIds.Contains(m.ConversationId))
            .SortByDescending(m => m.SentAt)
            .Group(
                m => m.ConversationId, // Nhóm theo ConversationId 
                g => new
                {
                    ConversationId = g.Key,
                    LastMessage = g.First() // Lấy toàn bộ document đầu tiên
                }
            );

        var results = await aggregation.ToListAsync();

        return results.ToDictionary(r => r.ConversationId, r => r.LastMessage);
    }

    public async Task<long> DeleteManyByConversationIdsAsync(List<int> conversationIds)
    {
        if (conversationIds == null || !conversationIds.Any())
        {
            return 0;
        }

        var filter = Builders<Messages>.Filter.In(m => m.ConversationId, conversationIds);
        var result = await _collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public Task<List<Messages>> GetOldRecalledMessagesAsync(DateTime cutoffTime)
    {
        var filter = Builders<Messages>.Filter.And(
            Builders<Messages>.Filter.Eq(m => m.IsDeleted, true),
            Builders<Messages>.Filter.Lt(m => m.DeletedAt, cutoffTime)
        );
        return _collection.Find(filter).ToListAsync();
    }

    public async Task<long> DeleteManyByIdsAsync(List<string> messageIds)
    {
        if (messageIds == null || !messageIds.Any())
        {
            return 0;
        }

        var filter = Builders<Messages>.Filter.In(m => m.Id, messageIds);
        var result = await _collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<Dictionary<int, long>> GetUnreadCountsForConversationsAsync(List<int> conversationIds, Guid userId)
    {
        if (conversationIds == null || !conversationIds.Any())
        {
            return new Dictionary<int, long>();
        }

        var filter = Builders<Messages>.Filter.And(
            Builders<Messages>.Filter.In(m => m.ConversationId, conversationIds),
            Builders<Messages>.Filter.Ne(m => m.Sender.UserId, userId),
            Builders<Messages>.Filter.Not(
                Builders<Messages>.Filter.ElemMatch(m => m.ReadBy, r => r.UserId == userId)
            ),
            Builders<Messages>.Filter.Ne(m => m.MessageType, EnumMessageType.SystemNotification),
            Builders<Messages>.Filter.Ne(m => m.IsDeleted, true)
        );

        var aggregation = _collection.Aggregate()
            .Match(filter)
            .Group(
                m => m.ConversationId,
                g => new
                {
                    ConversationId = g.Key,
                    UnreadCount = g.LongCount()
                }
            );

        var results = await aggregation.ToListAsync();

        return results.ToDictionary(r => r.ConversationId, r => r.UnreadCount);
    }

    public async Task<DomainPagedResult<Messages>> SearchMessagesAsync(int conversationId, string searchTerm, int pageNumber, int pageSize)
    {
        var filter = Builders<Messages>.Filter.And(
            Builders<Messages>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<Messages>.Filter.Text(searchTerm)
        );

        var totalRecordsTask = _collection.CountDocumentsAsync(filter);

        var itemsTask = _collection.Find(filter)
            .Sort(Builders<Messages>.Sort.MetaTextScore("textScore")) // Sắp xếp theo điểm văn bản
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        await Task.WhenAll(totalRecordsTask, itemsTask);

        return new DomainPagedResult<Messages>(await itemsTask, await totalRecordsTask);
    }

    public async Task<List<Messages>> GetMessageContextAsync(int conversationId, string targetMessageId, int countBefore, int countAfter)
    {
        var filterBuilder = Builders<Messages>.Filter;
        var conversationFilter = filterBuilder.Eq(m => m.ConversationId, conversationId);

        var beforeFilter = conversationFilter & filterBuilder.Lt(m => m.Id, targetMessageId);
        var messagesBefore = await _collection.Find(beforeFilter)
            .Sort(Builders<Messages>.Sort.Descending(m => m.Id))
            .Limit(countBefore)
            .ToListAsync();

        var afterAndTargetFilter = conversationFilter & filterBuilder.Gte(m => m.Id, targetMessageId);
        var messagesAfterAndTarget = await _collection.Find(afterAndTargetFilter)
            .Sort(Builders<Messages>.Sort.Ascending(m => m.Id))
            .Limit(countAfter + 1) 
            .ToListAsync();

        var allMessages = messagesBefore.AsEnumerable().Reverse()
                          .Concat(messagesAfterAndTarget)
                          .ToList();

        return allMessages;
    }

    public async Task<bool> HasNewerMessagesAsync(int conversationId, string newestMessageIdInBatch)
    {
        var filter = Builders<Messages>.Filter.And(
            Builders<Messages>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<Messages>.Filter.Gt(m => m.Id, newestMessageIdInBatch)
        );
        return await _collection.Find(filter).Limit(1).AnyAsync();
    }
}
