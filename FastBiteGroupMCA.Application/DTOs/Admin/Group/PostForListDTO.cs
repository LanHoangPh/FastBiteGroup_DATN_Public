namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class PostForListDTO
{
    public int PostId { get; set; }
    public string? Title { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsPinned { get; set; }
    public bool IsDeleted { get; set; }
}
