namespace FastBiteGroupMCA.Application.DTOs.Post;

public class CreatePostDTO
{
    public string? Title { get; set; }
    public string ContentJson { get; set; } = string.Empty;
    public List<int>? AttachmentFileIds { get; set; } = new();
}
