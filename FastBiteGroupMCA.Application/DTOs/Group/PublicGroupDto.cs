using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Group
{
    public class PublicGroupDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string GroupAvatarUrl { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public GroupTypeApiDto GroupType { get; set; }
    }
}
