using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates
{
    public record GroupInvitationEventData(Group Group, AppUser Inviter);

    // Class triển khai "công thức" cho Mời vào nhóm
    public class GroupInvitationNotificationTemplate : INotificationTemplate<GroupInvitationEventData>
    {
        public EnumNotificationType NotificationType => EnumNotificationType.GroupInvitation;

        public string BuildContent(GroupInvitationEventData data)
            => $"{data.Inviter.FullName} đã mời bạn tham gia nhóm '{data.Group.GroupName}'.";

        public RelatedObjectInfo BuildRelatedObject(GroupInvitationEventData data)
            => new()
            {
                ObjectType = EnumNotificationObjectType.Group,
                ObjectId = data.Group.GroupID.ToString(),
                NavigateUrl = $"/groups/{data.Group.GroupID}"
            };
    }
}
