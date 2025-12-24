using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Post;

public class UpdatePostDto
{
    public string? Title { get; set; }

    [Required]
    public string ContentJson { get; set; } = string.Empty;
}
