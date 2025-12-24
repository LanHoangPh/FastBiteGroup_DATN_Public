namespace FastBiteGroupMCA.Application.DTOs.Hubs;

public class IncomingCallDto
{
    public Guid VideoCallSessionId { get; set; }
    public int ConversationId { get; set; }
    public UserProfileDto Caller { get; set; } = null!;
}
