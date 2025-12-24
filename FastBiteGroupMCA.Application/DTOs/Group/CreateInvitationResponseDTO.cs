namespace FastBiteGroupMCA.Application.DTOs.Group;

public class CreateInvitationResponseDTO
{
    public string InvitationUrl { get; set; } = string.Empty;
    public string InvitationCode { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
