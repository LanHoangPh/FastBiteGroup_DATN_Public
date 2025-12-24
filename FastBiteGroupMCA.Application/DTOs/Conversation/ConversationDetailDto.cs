using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Conversation;

public class ConversationDetailDto
{
    public int ConversationId { get; set; }
    public Guid? GroupId { get; set; }
    public EnumConversationType ConversationType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    // Thông tin người đối thoại (nếu là chat 1-1)
    public ConversationPartnerDto? Partner { get; set; }

    // Trang đầu tiên của lịch sử tin nhắn
    public PagedResult<MessageDTO> MessagesPage { get; set; } = null!;
}
