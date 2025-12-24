using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Conversation;

public class ConversationListItemDTO
{
    public int ConversationId { get; set; }
    // --- BỔ SUNG TRƯỜNG NÀY ---
    /// <summary>
    /// ID của Group liên quan (nếu đây là group chat).
    /// </summary>
    public Guid? GroupId { get; set; }
    public EnumConversationType ConversationType { get; set; }

    // Thông tin hiển thị (tên, avatar)
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public EnumUserPresenceStatus? PartnerPresenceStatus { get; set; }

    // Thông tin về tin nhắn cuối cùng
    public string? LastMessagePreview { get; set; }
    public EnumMessageType? LastMessageType { get; set; }
    public DateTime? LastMessageTimestamp { get; set; }

    // Thông tin về trạng thái chưa đọc
    public int UnreadCount { get; set; }
}
