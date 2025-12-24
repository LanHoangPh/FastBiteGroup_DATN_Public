namespace FastBiteGroupMCA.Application.DTOs.Poll;

public class PollDetailDTO
{
    public int PollId { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool IsClosed => ClosesAt.HasValue && ClosesAt.Value <= DateTime.UtcNow;
    public DateTime? ClosesAt { get; set; }
    public int TotalVoteCount { get; set; }
    public List<PollOptionDetailDTO> Options { get; set; } = new();
}
