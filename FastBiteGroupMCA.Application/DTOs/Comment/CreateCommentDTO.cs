namespace FastBiteGroupMCA.Application.DTOs.Comment;

public class CreateCommentDTO
{
    public string Content { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
}
