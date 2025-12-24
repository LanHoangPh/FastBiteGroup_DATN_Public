using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class GroupDetailsDTO
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GroupAvatarUrl { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public GroupTypeApiDto GroupType { get; set; }
    public EnumGroupPrivacy Privacy { get; set; }
    public bool IsArchived { get; set; }
    public bool CanEdit { get; set; }
    public bool CanArchive { get; set; }
    public bool CanDelete { get; set; }
    // --- BỔ SUNG CỜ MỚI ---
    /// <summary>
    /// Người dùng hiện tại có quyền mời thành viên mới vào nhóm này không?
    /// </summary>
    public bool CanInviteMembers { get; set; }
}
