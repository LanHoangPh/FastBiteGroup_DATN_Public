using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Auth;

public class ResetPasswordDTO
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string ResetToken { get; set; } = string.Empty;
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
