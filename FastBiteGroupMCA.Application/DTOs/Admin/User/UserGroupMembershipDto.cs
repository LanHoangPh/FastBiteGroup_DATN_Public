namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class UserGroupMembershipDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string UserRoleInGroup { get; set; } = string.Empty;
}
