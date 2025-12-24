using FastBiteGroupMCA.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class UpdateMemberRoleDTO
{
    // Sử dụng Enum trực tiếp để ASP.NET Core tự động validate
    [Required]
    public EnumGroupRole NewRole { get; set; }
}
