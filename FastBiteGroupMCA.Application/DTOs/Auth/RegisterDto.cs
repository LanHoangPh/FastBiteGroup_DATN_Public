using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Auth;

public class RegisterDto
{
    //[Required]
    //[StringLength(256, MinimumLength = 3)]
    //public string UserName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 6)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    [Required]
    public DateTime DateOfBirth { get; set; }
}
