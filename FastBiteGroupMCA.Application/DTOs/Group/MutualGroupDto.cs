namespace FastBiteGroupMCA.Application.DTOs.Group;

public class MutualGroupDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? GroupAvatarUrl { get; set; }

    /// <summary>
    /// Cho biết người dùng hiện tại có quyền xóa người đối thoại (partner)
    /// ra khỏi nhóm chung này hay không.
    /// </summary>
    public bool CanKickPartner { get; set; }
}
