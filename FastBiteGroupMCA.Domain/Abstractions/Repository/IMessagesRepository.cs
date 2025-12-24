using FastBiteGroupMCA.Domain.Abstractions.Repository.MongoDb;
using MongoDB.Driver;

namespace FastBiteGroupMCA.Domain.Abstractions.Repository;

public interface IMessagesRepository : IMongoRepository<Messages>
{
    /// <summary>
    /// Lấy một danh sách tin nhắn trong một cuộc hội thoại,
    /// được tạo ra trước một tin nhắn cụ thể (dùng để tải thêm).
    /// </summary>
    /// <param name="conversationId">ID của cuộc hội thoại.</param>
    /// <param name="beforeMessageId">ID của tin nhắn làm mốc. Bỏ trống nếu là lần tải đầu tiên.</param>
    /// <param name="limit">Số lượng tin nhắn cần lấy.</param>
    /// <returns>Danh sách các tin nhắn.</returns>
    Task<List<Messages>> GetMessagesBeforeAsync(int conversationId, string? beforeMessageId, int limit);

    /// <summary>
    /// Kiểm tra xem có tin nhắn nào cũ hơn tin nhắn có ID được cung cấp hay không.
    /// </summary>
    /// <param name="conversationId">ID của cuộc hội thoại.</param>
    /// <param name="oldestMessageIdInBatch">ID của tin nhắn cũ nhất trong lô vừa tải.</param>
    /// <returns>True nếu có tin nhắn cũ hơn, ngược lại False.</returns>
    Task<bool> HasOlderMessagesAsync(int conversationId, string oldestMessageIdInBatch);

    /// <summary>
    /// Đánh dấu nhiều tin nhắn là đã đọc bởi một người dùng.
    /// Sử dụng $addToSet để tránh thêm trùng lặp.
    /// </summary>
    /// <returns>Số lượng document đã được cập nhật.</returns>
    Task<long> MarkMessagesAsReadAsync(IEnumerable<string> messageIds, ReadReceiptInfo readerInfo);
    /// <summary>
    /// Cập nhật một tin nhắn dựa trên điều kiện lọc.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="update"></param>
    /// <param name="returnNewDocument"></param>
    /// <returns></returns>
    Task<Messages> FindOneAndUpdateAsync(FilterDefinition<Messages> filter, UpdateDefinition<Messages> update, bool returnNewDocument = true);
    /// <summary>
    /// Lấy tin nhắn cuối cùng cho một danh sách các cuộc hội thoại.
    /// </summary>
    /// <returns>Một Dictionary với Key là ConversationId và Value là tin nhắn cuối cùng.</returns>
    Task<Dictionary<int, Messages>> GetLastMessageForConversationsAsync(IEnumerable<int> conversationIds);

    /// <summary>
    /// Xóa nhiều tin nhắn dựa trên danh sách ConversationIds.
    /// </summary>
    /// <param name="conversationIds"></param>
    /// <returns></returns>
    Task<long> DeleteManyByConversationIdsAsync(List<int> conversationIds);

    /// <summary>
    /// Lấy danh sách các tin nhắn đã bị thu hồi và quá một khoảng thời gian nhất định.
    /// </summary>
    Task<List<Messages>> GetOldRecalledMessagesAsync(DateTime cutoffTime);

    /// <summary>
    /// Xóa vĩnh viễn nhiều tin nhắn dựa trên danh sách ID.
    /// </summary>
    Task<long> DeleteManyByIdsAsync(List<string> messageIds);
    /// <summary>
    /// Lấy số lượng tin nhắn chưa đọc cho mỗi cuộc hội thoại trong danh sách.
    /// </summary>
    /// <param name="conversationIds"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<Dictionary<int, long>> GetUnreadCountsForConversationsAsync(List<int> conversationIds, Guid userId);
    /// <summary>
    /// Tìm kiếm tin nhắn trong một cuộc hội thoại dựa trên từ khóa, có phân trang.
    /// </summary>
    Task<DomainPagedResult<Messages>> SearchMessagesAsync(int conversationId, string searchTerm, int pageNumber, int pageSize);
    /// <summary>
    /// Lấy ngữ cảnh của một tin nhắn cụ thể trong cuộc hội thoại,
    /// </summary>
    Task<List<Messages>> GetMessageContextAsync(int conversationId, string targetMessageId, int countBefore, int countAfter);
    /// <summary>
    /// Kiểm tra xem có tin nhắn nào mới hơn một tin nhắn cụ thể trong cuộc hội thoại không.
    /// </summary>
    Task<bool> HasNewerMessagesAsync(int conversationId, string newestMessageIdInBatch);
}
