namespace FastBiteGroupMCA.Application.DTOs.Invitation;

public class GroupInvitationDTO
{
    public int InvitationId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupAvatarUrl { get; set; } = string.Empty;
    public string InvitedByName { get; set; } = string.Empty;
}
