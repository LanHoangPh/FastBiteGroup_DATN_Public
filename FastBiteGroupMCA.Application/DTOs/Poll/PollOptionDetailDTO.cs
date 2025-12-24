namespace FastBiteGroupMCA.Application.DTOs.Poll;

public class PollOptionDetailDTO
{
    public int OptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public List<VoterDTO> Voters { get; set; } = new();
    public bool HasVotedByCurrentUser { get; set; }
}
