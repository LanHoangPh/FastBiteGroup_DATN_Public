namespace FastBiteGroupMCA.Application.DTOs.Message;
/// <summary>
/// Dữ liệu trả về cho một chunk lịch sử tin nhắn.
/// </summary>
public class MessageHistoryResponseDTO
{
    /// <summary>
    /// Danh sách các tin nhắn.
    /// </summary>
    public List<MessageDTO> Messages { get; set; } = new();
    /// <summary>
    /// Cờ báo hiệu có còn tin nhắn cũ hơn để tải hay không.
    /// </summary>
    public bool HasMore { get; set; }
    // <summary>
    /// Con trỏ (cursor) để sử dụng cho lần tải tiếp theo.
    /// Đây là ID của tin nhắn cũ nhất trong danh sách hiện tại.
    /// </summary>
    public string? NextCursor { get; set; }
}
