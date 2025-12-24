using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class DeactivateAccountDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
}
