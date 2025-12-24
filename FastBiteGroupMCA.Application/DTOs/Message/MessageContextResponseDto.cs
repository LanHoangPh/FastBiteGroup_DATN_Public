namespace FastBiteGroupMCA.Application.DTOs.Message;

public class MessageContextResponseDto
{
    public List<MessageDTO> Messages { get; set; } = new();

    // ID của tin nhắn mục tiêu để frontend có thể highlight
    public string TargetMessageId { get; set; } = string.Empty;

    // Cờ cho biết có còn tin nhắn cũ hơn/mới hơn ở 2 đầu hay không
    public bool HasOlderMessages { get; set; }
    public bool HasNewerMessages { get; set; }
}
