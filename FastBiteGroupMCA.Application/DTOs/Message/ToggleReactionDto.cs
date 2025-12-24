using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Message;

public class ToggleReactionDto
{
    [Required]
    public string ReactionCode { get; set; } = string.Empty; // Ví dụ: "👍", "❤️"
}
