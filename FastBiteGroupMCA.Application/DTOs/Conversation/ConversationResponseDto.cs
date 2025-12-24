namespace FastBiteGroupMCA.Application.DTOs.Conversation;

public class ConversationResponseDto
{
    public int ConversationId { get; set; }
    public ConversationPartnerDto Partner { get; set; } = default!;
    public bool WasCreated { get; set; } // Cờ cho biết có tạo mới hay không
}
