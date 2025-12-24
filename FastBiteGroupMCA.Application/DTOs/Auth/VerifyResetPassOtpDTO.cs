using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Auth;

public class VerifyResetPassOtpDTO
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Otp { get; set; } = string.Empty;
}
