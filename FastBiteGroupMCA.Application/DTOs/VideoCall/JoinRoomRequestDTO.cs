using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.VideoCall;

public class JoinRoomRequestDTO
{
    [Required]
    public int ConversationId { get; set; }
}
