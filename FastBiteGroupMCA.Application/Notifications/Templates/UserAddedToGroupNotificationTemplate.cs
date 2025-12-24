using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates
{
    public record UserAddedToGroupEventData(Group Group, AppUser AddedByUser);
    public class UserAddedToGroupNotificationTemplate : INotificationTemplate<UserAddedToGroupEventData>
    {
        public EnumNotificationType NotificationType => EnumNotificationType.UserAddedToGroup;

        public string BuildContent(UserAddedToGroupEventData data)
            => $"{data.AddedByUser.FullName} đã thêm bạn vào nhóm '{data.Group.GroupName}'.";

        public RelatedObjectInfo BuildRelatedObject(UserAddedToGroupEventData data)
            => new()
            {
                ObjectType = EnumNotificationObjectType.Group,
                ObjectId = data.Group.GroupID.ToString(),
                NavigateUrl = $"/groups/{data.Group.GroupID}"
            };
    }
}
