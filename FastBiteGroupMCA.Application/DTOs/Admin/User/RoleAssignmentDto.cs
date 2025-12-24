using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class RoleAssignmentDto
{
    [Required]
    public string RoleName { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
