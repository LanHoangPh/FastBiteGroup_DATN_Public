using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class GroupAdminMemberDTO
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public EnumGroupRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
