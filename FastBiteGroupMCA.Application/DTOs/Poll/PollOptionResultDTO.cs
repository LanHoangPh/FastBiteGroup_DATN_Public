namespace FastBiteGroupMCA.Application.DTOs.Poll;

public class PollOptionResultDTO
{
    public int PollOptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    // Gửi danh sách ID người đã vote để FE biết ai đã vote và tô sáng lựa chọn của user hiện tại
    public List<Guid> VotedByUsers { get; set; } = new();
}
