using FastBiteGroupMCA.Application.DTOs.Message;

namespace FastBiteGroupMCA.Application.DTOs.Poll;

public class CreatePollResponseDTO
{
    public int PollId { get; set; }
    // Chúng ta cũng nên trả về Message DTO để client không cần làm gì thêm
    public MessageDTO PollMessage { get; set; } = null!;
}
