using FastBiteGroupMCA.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class CreateCommunityGroupAsAdminDto
{
    [Required]
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Required]
    public EnumGroupPrivacy Privacy { get; set; } // Chỉ chấp nhận Public hoặc Private

    [Required]
    public List<Guid> InitialAdminUserIds { get; set; } = new();
}
