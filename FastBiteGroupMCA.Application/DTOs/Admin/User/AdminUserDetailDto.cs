namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class AdminUserDetailDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Bio { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } 
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // thêm cái này
    public List<string> Roles { get; set; } = new(); 
    public UserStatsDTO Stats { get; set; } = new();
    public List<UserGroupMembershipDto> GroupMemberships { get; set; } = new();
    public List<UserRecentPostDto> RecentPosts { get; set; } = new(); 
}
