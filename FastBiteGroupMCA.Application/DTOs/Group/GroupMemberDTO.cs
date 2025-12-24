namespace FastBiteGroupMCA.Application.DTOs.Group
{
    public class GroupMemberDTO
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
