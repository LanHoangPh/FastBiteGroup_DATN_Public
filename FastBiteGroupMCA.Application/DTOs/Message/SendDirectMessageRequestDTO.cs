using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Message;

public class SendDirectMessageRequestDTO
{
    [Required]
    public Guid RecipientId { get; set; } // userId muốn trò chuyện 

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty; // hello 
}
