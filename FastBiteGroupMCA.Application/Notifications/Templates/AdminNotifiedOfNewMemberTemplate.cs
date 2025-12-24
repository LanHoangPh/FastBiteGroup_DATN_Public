using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;


public class AdminNotifiedOfNewMemberTemplate : INotificationTemplate<UserAddedToGroupByAdminEventData>
{
    // Bạn có thể dùng một loại notification chung hoặc tạo một loại mới
    public EnumNotificationType NotificationType => EnumNotificationType.SystemAnnouncement;

    public string BuildContent(UserAddedToGroupByAdminEventData data)
        => $"Hệ thống: Quản trị viên '{data.AdminName}' vừa thêm người dùng '{data.AddedUserName}' vào nhóm '{data.GroupName}' của bạn.";

    public RelatedObjectInfo BuildRelatedObject(UserAddedToGroupByAdminEventData data)
        => new()
        {
            ObjectType = EnumNotificationObjectType.Group,
            ObjectId = data.GroupId.ToString(),
            NavigateUrl = $"/groups/{data.GroupId}/members"
        };
}
