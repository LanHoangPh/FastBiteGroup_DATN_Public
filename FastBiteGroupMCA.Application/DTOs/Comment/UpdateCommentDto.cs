using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Comment;

public class UpdateCommentDto
{
    [Required]
    public string Content { get; set; } = string.Empty;
}
