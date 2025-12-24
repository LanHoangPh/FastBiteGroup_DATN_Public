using FastBiteGroupMCA.Domain.Entities;

namespace FastBiteGroupMCA.Application.DTOs.Message;

public class ToggleReactionResponseDto
{
    public string MessageId { get; set; } = string.Empty;
    public List<Reaction> NewReactions { get; set; } = new(); // Trạng thái mới của các reaction
}
