namespace FastBiteGroupMCA.Application.DTOs.Message;

public class MessageSenderDTO
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
