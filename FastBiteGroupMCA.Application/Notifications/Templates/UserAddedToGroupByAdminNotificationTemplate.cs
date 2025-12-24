using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;
public record UserAddedToGroupByAdminEventData(
    string AddedUserName,
    string GroupName,
    Guid GroupId,
    string AdminName
);
public class UserAddedToGroupByAdminNotificationTemplate : INotificationTemplate<UserAddedToGroupByAdminEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.UserAddedToGroup; 

    public string BuildContent(UserAddedToGroupByAdminEventData data)
        => $"Bạn đã được quản trị viên '{data.AdminName}' thêm vào nhóm '{data.GroupName}'.";

    public RelatedObjectInfo BuildRelatedObject(UserAddedToGroupByAdminEventData data)
        => new()
        {
            ObjectType = EnumNotificationObjectType.Group,
            ObjectId = data.GroupId.ToString(),
            NavigateUrl = $"/groups/{data.GroupId}"
        };
}

