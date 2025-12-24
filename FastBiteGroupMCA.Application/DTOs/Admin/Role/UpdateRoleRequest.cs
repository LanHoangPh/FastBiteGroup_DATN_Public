using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Role;

public class UpdateRoleRequest
{
    [Required]
    public string NewRoleName { get; set; } = string.Empty;
}
