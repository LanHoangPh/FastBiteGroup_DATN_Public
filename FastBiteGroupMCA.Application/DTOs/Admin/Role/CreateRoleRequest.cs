using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Role;

public class CreateRoleRequest
{
    [Required]
    public string RoleName { get; set; } = string.Empty;
}
