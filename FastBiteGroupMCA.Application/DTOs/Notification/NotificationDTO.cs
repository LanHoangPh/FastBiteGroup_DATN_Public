using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Notification;

public class NotificationDTO
{
    public string Id { get; set; } // ID từ MongoDB là string
    public Guid UserId { get; set; }
    public EnumNotificationType Type { get; set; }
    public string ContentPreview { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public RelatedObjectInfo? RelatedObject { get; set; } 
}
