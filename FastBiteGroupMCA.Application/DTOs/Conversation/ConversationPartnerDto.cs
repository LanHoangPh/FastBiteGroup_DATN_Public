using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Conversation;

public class ConversationPartnerDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public EnumUserPresenceStatus PresenceStatus { get; set; }
    // --- BỔ SUNG THUỘC TÍNH MỚI ---
    /// <summary>
    /// Số lượng nhóm chung giữa người dùng hiện tại và người này.
    /// </summary>
    public int MutualGroupsCount { get; set; }
}
