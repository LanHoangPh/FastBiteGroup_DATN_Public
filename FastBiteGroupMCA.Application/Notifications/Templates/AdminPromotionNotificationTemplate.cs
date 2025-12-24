using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

/// <summary>
/// Dữ liệu cần thiết để tạo thông báo thăng cấp admin.
/// </summary>
public record AdminPromotionEventData(Group Group);

/// <summary>
/// Template để xây dựng nội dung thông báo khi một người dùng được thăng làm Admin.
/// </summary>
public class AdminPromotionNotificationTemplate : INotificationTemplate<AdminPromotionEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.AdminPromotion;

    public string BuildContent(AdminPromotionEventData data)
    {
        return $"Bạn đã được thăng làm Quản trị viên trong nhóm '{data.Group.GroupName}'.";
    }

    public RelatedObjectInfo BuildRelatedObject(AdminPromotionEventData data)
    {
        // Link sẽ điều hướng người dùng đến trang chủ của nhóm
        return new()
        {
            ObjectType = EnumNotificationObjectType.Group,
            ObjectId = data.Group.GroupID.ToString(),
            NavigateUrl = $"/groups/{data.Group.GroupID}"
        };
    }
}
