namespace FastBiteGroupMCA.Application.DTOs.User
{
    public class UserSearchResultDTO
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
