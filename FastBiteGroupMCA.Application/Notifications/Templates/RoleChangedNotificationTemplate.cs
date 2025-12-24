using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record RoleChangedEventData(Group Group, string NewRoleName);

public class RoleChangedNotificationTemplate : INotificationTemplate<RoleChangedEventData>
{
    // Có thể cần thêm một EnumNotificationType mới, ví dụ: RoleChanged
    public EnumNotificationType NotificationType => EnumNotificationType.SystemAnnouncement;

    public string BuildContent(RoleChangedEventData data)
    {
        return $"Vai trò của bạn trong nhóm '{data.Group.GroupName}' đã được thay đổi thành {data.NewRoleName}.";
    }

    public RelatedObjectInfo BuildRelatedObject(RoleChangedEventData data)
    {
        // Điều hướng đến trang thành viên của nhóm
        return new()
        {
            ObjectType = EnumNotificationObjectType.Group,
            ObjectId = data.Group.GroupID.ToString(),
            NavigateUrl = $"/groups/{data.Group.GroupID}/members"
        };
    }
}
