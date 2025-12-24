using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record InvitationAcceptedEventData(AppUser AcceptedUser, Group Group);

// 2. Template để tạo thông báo
public class InvitationAcceptedNotificationTemplate : INotificationTemplate<InvitationAcceptedEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.GroupInvitation; // Có thể dùng chung type

    public string BuildContent(InvitationAcceptedEventData data)
        => $"{data.AcceptedUser.FullName} đã chấp nhận lời mời tham gia nhóm '{data.Group.GroupName}'.";

    public RelatedObjectInfo BuildRelatedObject(InvitationAcceptedEventData data)
        => new()
        {
            ObjectType = EnumNotificationObjectType.Group,
            ObjectId = data.Group.GroupID.ToString(),
            // Link có thể trỏ tới trang thành viên của nhóm
            NavigateUrl = $"/groups/{data.Group.GroupID}/members"
        };
}
