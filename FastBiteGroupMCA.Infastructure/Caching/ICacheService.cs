namespace FastBiteGroupMCA.Infastructure.Caching;

public interface ICacheService
{
    /// <summary>
    /// Lấy một giá trị từ cache.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của đối tượng.</typeparam>
    /// <param name="key">Khóa của cache.</param>
    /// <returns>Đối tượng đã được deserialize hoặc null nếu không tìm thấy.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Lưu một giá trị vào cache với thời gian hết hạn.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// Xóa một giá trị khỏi cache.
    /// </summary>
    Task RemoveAsync(string key);

    // Bạn có thể thêm các phương thức chuyên dụng khác ở đây trong tương lai
    // Ví dụ: Task<long> IncrementAsync(string key);
}
