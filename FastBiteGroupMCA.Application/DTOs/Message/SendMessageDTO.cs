namespace FastBiteGroupMCA.Application.DTOs.Message;
/// <summary>
/// Dữ liệu để gửi một tin nhắn mới.
/// </summary>
public class SendMessageDTO
{
    //public int ConversationId { get; set; }
    /// <summary>
    /// Nội dung tin nhắn văn bản. Bắt buộc nếu không có tệp đính kèm.
    /// </summary>
    /// <example>Dự án này khi nào thì xong vậy mọi người?</example>
    public string? Content { get; set; }

    /// <summary>
    /// (Tùy chọn) ID của tin nhắn cha đang được trả lời (reply).
    /// </summary>
    public string? ParentMessageId { get; set; } // ID của tin nhắn được trả lời
    // --- BỔ SUNG THUỘC TÍNH NÀY ---
    /// <summary>
    /// (Tùy chọn) Danh sách ID của các file đã được upload trước đó.
    /// </summary>
    public List<int>? AttachmentFileIds { get; set; }
}
