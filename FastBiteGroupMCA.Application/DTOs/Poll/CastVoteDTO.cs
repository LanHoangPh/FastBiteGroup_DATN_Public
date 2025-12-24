using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Poll;

public class CastVoteDTO
{
    [Required]
    public int PollOptionId { get; set; }
}
