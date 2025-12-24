using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class UserStatusChangedDTO
{
    public string UserId { get; set; } = string.Empty;
    public EnumUserPresenceStatus PresenceStatus { get; set; }
}
