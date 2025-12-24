namespace FastBiteGroupMCA.Domain.Entities;
/// <summary>
/// mở rộng cho sau này
/// </summary>
public class AdminAuditLog
{
    public long Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string AdminFullName { get; set; } = string.Empty;
    public EnumAdminActionType ActionType { get; set; }
    public EnumTargetEntityType TargetEntityType { get; set; } 
    public string TargetEntityId { get; set; } = string.Empty;
    public string? Details { get; set; } // Lý do hoặc thông tin thêm (dạng JSON hoặc text)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? BatchId { get; set; }

    public virtual AppUser? AdminUser { get; set; }
}
