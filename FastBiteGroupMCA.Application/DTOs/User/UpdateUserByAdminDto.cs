using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FastBiteGroupMCA.Application.DTOs.User;

/// <summary>
/// DTO cho Admin cập nhật thông tin người dùng
/// Chỉ bao gồm các trường cơ bản mà Admin được phép chỉnh sửa
/// </summary>
public class UpdateUserByAdminDto
{
    [Required]
    public string FisrtName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Bio { get; set; }
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
}
