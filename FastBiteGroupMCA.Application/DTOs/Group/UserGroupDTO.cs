using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;
using System.Text.Json.Serialization;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class UserGroupDTO
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty; 
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; } 
    public GroupTypeApiDto GroupType { get; set; }
    public EnumGroupPrivacy Privacy { get; set; }
    public int ConversationId { get; set; }
    // BỔ SUNG CÁC TRƯỜNG MỚI
    public int MemberCount { get; set; }
    public bool IsOwner { get; set; }
    public bool IsAdmin { get; set; }
}
