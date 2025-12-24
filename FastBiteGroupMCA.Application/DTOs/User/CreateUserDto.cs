using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.User
{
    public class CreateUserDto
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public bool IsActive { get; set; } = true;
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
