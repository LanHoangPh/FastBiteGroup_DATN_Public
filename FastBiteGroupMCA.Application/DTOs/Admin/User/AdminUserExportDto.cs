using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class AdminUserExportDto
{
    [Display(Name = "ID Người dùng")]
    public Guid UserId { get; set; }

    [Display(Name = "Họ và Tên")]
    public string? FullName { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Tên đăng nhập")]
    public string? UserName { get; set; }

    [Display(Name = "Vai trò")]
    public string Roles { get; set; } = string.Empty; // Chuyển List<string> thành string

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = string.Empty; // Dùng chuỗi cho dễ đọc

    [Display(Name = "Ngày tạo")]
    public DateTime CreatedAt { get; set; }
}
