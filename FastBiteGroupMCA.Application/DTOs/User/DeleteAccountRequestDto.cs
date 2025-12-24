using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class DeleteAccountRequestDto
{
    [Required] public string Password { get; set; } = string.Empty;
}
