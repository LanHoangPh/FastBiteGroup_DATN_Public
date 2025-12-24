using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Message;

public class MarkAsReadDTO
{
    [Required]
    public int ConversationId { get; set; }

    [Required]
    [MinLength(1)]
    public List<string> MessageIds { get; set; } = new();
}
