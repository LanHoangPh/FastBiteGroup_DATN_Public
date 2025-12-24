namespace FastBiteGroupMCA.Application.DTOs.User
{
    public class UserSearchDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
