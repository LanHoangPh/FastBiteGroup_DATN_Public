using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class GroupMemberSearchResultDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public EnumGroupRole RoleInGroup { get; set; } // << Quan trọng: vai trò hiện tại của họ
}
