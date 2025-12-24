using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Auth;

public class RefreshTokenDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
