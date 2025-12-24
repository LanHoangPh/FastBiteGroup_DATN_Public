using FastBiteGroupMCA.Domain.Abstractions.Repository.MongoDb;

namespace FastBiteGroupMCA.Domain.Abstractions.Repository;

public interface INotificationsRepository : IMongoRepository<Notifications>
{
    /// <summary>
    /// Tìm một thông báo theo ID, xác thực nó thuộc về user, và cập nhật IsRead = true.
    /// </summary>
    /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy hoặc đã được đọc.</returns>
    Task<bool> FindAndMarkAsReadAsync(string notificationId, Guid userId);
    /// <summary>
    /// Đánh dấu tất cả thông báo của user là đã đọc, trả về số bản ghi cập nhật.
    /// </summary>
    Task<long> MarkAllAsReadAsync(Guid userId);
}
