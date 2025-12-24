namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class UserRecentPostDto
{
    public int PostId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}