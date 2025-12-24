using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Group
{
    public class GroupMemberDetailDTO
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public EnumGroupRole Role { get; set; }
    }
}
