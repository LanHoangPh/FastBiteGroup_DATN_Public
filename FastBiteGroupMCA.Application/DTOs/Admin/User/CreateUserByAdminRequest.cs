using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class CreateUserByAdminRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; }= string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string UserName { get; set; } = string.Empty; // Tên đăng nhập của user mới
    [Required]
    public string RoleName { get; set; } = string.Empty; // Admin sẽ chọn vai trò cho user mới
}
