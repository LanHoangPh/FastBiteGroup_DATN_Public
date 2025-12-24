using FastBiteGroupMCA.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class CreateGroupAsAdminRequest
{
    [Required(ErrorMessage = "Tên nhóm không được để trống.")]
    [StringLength(100)]
    public string GroupName { get; set; } = string.Empty;

    public string? Description { get; set; }

    // BỔ SUNG: Cho phép Admin chọn loại nhóm
    [Required(ErrorMessage = "Vui lòng chọn loại nhóm.")]
    public EnumGroupType GroupType { get; set; }

    [Required(ErrorMessage = "Phải chỉ định ít nhất một quản trị viên cho nhóm.")]
    [MinLength(1, ErrorMessage = "Phải chỉ định ít nhất một quản trị viên cho nhóm.")]
    public List<Guid> InitialAdminUserIds { get; set; } = new();
}
