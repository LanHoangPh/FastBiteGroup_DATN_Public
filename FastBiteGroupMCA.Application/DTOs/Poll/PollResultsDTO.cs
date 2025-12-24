namespace FastBiteGroupMCA.Application.DTOs.Poll;

public class PollResultsDTO
{
    public int PollId { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool AllowMultipleChoices { get; set; }
    public DateTime? ClosesAt { get; set; } // Thêm trường này để UI biết poll đã đóng chưa
    public List<PollOptionResultDTO> Options { get; set; } = new();
}
