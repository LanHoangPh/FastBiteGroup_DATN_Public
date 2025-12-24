using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class AdminGroupDetailDTO
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GroupAvatarUrl { get; set; }
    public string? CreatorName { get; set; }
    public EnumGroupType GroupType { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDeleted { get; set; }

    public GroupStatsDTO Stats { get; set; } = new();
}
