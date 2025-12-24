using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class ContactDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public EnumUserPresenceStatus PresenceStatus { get; set; }
}
