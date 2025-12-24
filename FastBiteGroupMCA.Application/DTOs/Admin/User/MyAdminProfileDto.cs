using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class MyAdminProfileDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    // Thông tin bảo mật
    public LoginHistoryDto? LastLogin { get; set; }
    public List<LoginHistoryDto> LoginHistory { get; set; } = new();
    public List<AdminActionLogDto> RecentActions { get; set; } = new();
}
public class AdminActionLogDto
{
    public EnumAdminActionType ActionType { get; set; }
    public EnumTargetEntityType TargetEntityType { get; set; }
    public string TargetEntityId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
