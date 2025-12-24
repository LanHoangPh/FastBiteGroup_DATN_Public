namespace FastBiteGroupMCA.Application.DTOs.Poll;

public class VoterDTO
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
