namespace FastBiteGroupMCA.Application.DTOs.Group;

public class GroupPreviewDTO
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupAvatarUrl { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}
