using FastBiteGroupMCA.Domain.Enum;
using Microsoft.AspNetCore.Http;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class CreateCommunityGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EnumGroupPrivacy Privacy { get; set; }
    public IFormFile? AvatarFile { get; set; }
}
