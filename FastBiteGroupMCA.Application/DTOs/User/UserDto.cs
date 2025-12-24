namespace FastBiteGroupMCA.Application.DTOs.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public bool IsActive { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
