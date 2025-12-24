using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Domain.Entities;

public class AdminNotifications
{
    public long Id { get; set; } // Dùng long cho các bảng có thể phình to

    public EnumAdminNotificationType NotificationType { get; set; }

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    // Link để điều hướng khi Admin click vào, ví dụ: "/admin/reports/posts/123"
    public string? LinkTo { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // (Tùy chọn) Ghi lại ai đã gây ra sự kiện này
    public Guid? TriggeredByUserId { get; set; }
    public virtual AppUser? TriggeredByUser { get; set; }
}
