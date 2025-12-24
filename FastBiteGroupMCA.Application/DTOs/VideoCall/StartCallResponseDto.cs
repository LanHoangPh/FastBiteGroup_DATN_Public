namespace FastBiteGroupMCA.Application.DTOs.VideoCall;

public class StartCallResponseDto
{
    public Guid VideoCallSessionId { get; set; }
    public string LivekitToken { get; set; } = string.Empty;
    public string LivekitServerUrl { get; set; } = string.Empty;
}
