using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.AdminNotifications;

public class AdminNotificationDto
{
    public long Id { get; set; }
    public EnumAdminNotificationType NotificationType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LinkTo { get; set; }
    public bool IsRead { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? TriggeredByUserName { get; set; } // Thêm tên người gây ra sự kiện
}
