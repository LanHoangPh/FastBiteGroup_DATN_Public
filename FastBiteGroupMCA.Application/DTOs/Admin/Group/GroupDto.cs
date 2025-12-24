using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group
{
    public class GroupDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public EnumGroupType GroupType { get; set; }
        public string? GroupAvatarUrl { get; set; }
        public Guid CreatedByUserID { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsChatEnabled { get; set; }
        public bool IsPostsEnabled { get; set; }
    }
}
