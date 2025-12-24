namespace FastBiteGroupMCA.Application.DTOs.User;

public class PostAuthorDTO
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
