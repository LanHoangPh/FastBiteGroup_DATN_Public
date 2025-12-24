using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Auth;

public class VerifyOtpDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}
