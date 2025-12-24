using FastBiteGroupMCA.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class AddMemberAdminRequest
{
    [Required]
    public Guid UserId { get; set; }

    // Bạn có thể tùy chọn cho phép Admin tổng set luôn vai trò cho thành viên mới
    public EnumGroupRole Role { get; set; } = EnumGroupRole.Member;
}
