using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin;

public class AdminAuditLogDto
{
    public long Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string AdminFullName { get; set; } = string.Empty;
    public EnumAdminActionType ActionType { get; set; }
    public EnumTargetEntityType TargetEntityType { get; set; } 
    public string TargetEntityId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? BatchId { get; set; } // Thêm BatchId nếu cần thiết
    // THÊM THUỘC TÍNH NÀY
    /// <summary>
    /// Độ lệch múi giờ của client so với UTC, tính bằng phút (ví dụ: UTC+7 là -420).
    /// </summary>
    public int? TimezoneOffsetMinutes { get; set; }
}
