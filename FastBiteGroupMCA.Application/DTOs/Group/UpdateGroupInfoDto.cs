using FastBiteGroupMCA.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class UpdateGroupInfoDto
{
    [Required]
    [StringLength(100)]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public EnumGroupPrivacy Privacy { get; set; }
}
