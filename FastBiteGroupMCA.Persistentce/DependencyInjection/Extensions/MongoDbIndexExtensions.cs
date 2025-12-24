using MongoDB.Driver;

namespace FastBiteGroupMCA.Persistentce.DependencyInjection.Extensions;

public static class MongoDbIndexExtensions
{
    public static async Task CreateIndexesAsync(this IMongoDatabase database)
    {
        var messagesCollection = database.GetCollection<Messages>("messages");

        // 1. Tạo Text Index cho trường "content"
        var textIndexKey = Builders<Messages>.IndexKeys.Text(m => m.Content);
        var textIndexModel = new CreateIndexModel<Messages>(textIndexKey);
        await messagesCollection.Indexes.CreateOneAsync(textIndexModel);

        // 2. (Tùy chọn) Tạo các index khác để tối ưu hóa
        // Ví dụ: Index trên ConversationId và SentAt để tăng tốc độ lấy lịch sử tin nhắn
        var historyIndexKeys = Builders<Messages>.IndexKeys
            .Ascending(m => m.ConversationId)
            .Descending(m => m.SentAt);
        var historyIndexModel = new CreateIndexModel<Messages>(historyIndexKeys);
        await messagesCollection.Indexes.CreateOneAsync(historyIndexModel);

        var notificationsCollection = database.GetCollection<Notifications>("notifications");

        // Index để lấy danh sách thông báo của user, sắp xếp theo thời gian
        var userNotificationsIndex = Builders<Notifications>.IndexKeys
            .Ascending(n => n.UserId)
            .Descending(n => n.CreatedAt);
        await notificationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Notifications>(userNotificationsIndex)
        );

        // Index để đếm số thông báo chưa đọc
        var unreadCountIndex = Builders<Notifications>.IndexKeys
            .Ascending(n => n.UserId)
            .Ascending(n => n.IsRead);
        await notificationsCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<Notifications>(unreadCountIndex)
        );
    }
}
