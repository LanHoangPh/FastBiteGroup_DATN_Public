namespace FastBiteGroupMCA.Application.DTOs.Message;

public class GetMessagesQuery
{
    /// <summary>
    /// Dùng để tải các tin nhắn cũ hơn tin nhắn có ID này.
    /// Bỏ trống khi tải lần đầu.
    /// </summary>
    public string? BeforeMessageId { get; set; }

    /// <summary>
    /// Số lượng tin nhắn cần lấy.
    /// </summary>
    public int Limit { get; set; } = 50;
}
