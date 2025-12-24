using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class UserActivityDTO
{
    public EnumUserActivityType ActivityType { get; set; } 
    public string ContentPreview { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public int PostId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Url { get; set; } = string.Empty; // URL để điều hướng
}
