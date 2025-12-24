using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class UserStatusDto
{
    public Guid UserId { get; set; }
    public EnumUserPresenceStatus Status { get; set; }
}
