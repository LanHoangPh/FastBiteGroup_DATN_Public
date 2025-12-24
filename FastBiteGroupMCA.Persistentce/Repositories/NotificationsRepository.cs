using FastBiteGroupMCA.Persistentce.Repositories.MongoDb;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class NotificationsRepository : MongoRepository<Notifications>, INotificationsRepository
{
    public NotificationsRepository(IMongoDatabase database) : base(database)
    {

    }

    public async Task<bool> FindAndMarkAsReadAsync(string notificationId, Guid userId)
    {
        // Tạo một bộ lọc để tìm chính xác document cần cập nhật:
        // Phải đúng ID, đúng UserId, VÀ chưa được đọc (IsRead = false).
        var filter = Builders<Notifications>.Filter.And(
            Builders<Notifications>.Filter.Eq(doc => doc.Id, notificationId),
            Builders<Notifications>.Filter.Eq(doc => doc.UserId, userId),
            Builders<Notifications>.Filter.Eq(doc => doc.IsRead, false)
        );

        // Tạo một định nghĩa cập nhật để set IsRead = true
        var update = Builders<Notifications>.Update.Set(doc => doc.IsRead, true);

        // Thực thi lệnh cập nhật
        var result = await _collection.UpdateOneAsync(filter, update);

        // Trả về true nếu có chính xác 1 document được tìm thấy và chỉnh sửa
        return result.IsAcknowledged && result.ModifiedCount == 1;
    }

    public async Task<long> MarkAllAsReadAsync(Guid userId)
    {
        var filter = Builders<Notifications>.Filter.And(
            Builders<Notifications>.Filter.Eq(n => n.UserId, userId),
            Builders<Notifications>.Filter.Eq(n => n.IsRead, false));

        var update = Builders<Notifications>.Update.Set(n => n.IsRead, true);
        var result = await _collection.UpdateManyAsync(filter, update);
        return result.IsAcknowledged ? result.ModifiedCount : 0;
    }
}

