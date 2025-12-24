using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record MissedCallEventData(AppUser Caller, Conversation Conversation);

public class MissedCallNotificationTemplate : INotificationTemplate<MissedCallEventData>
{
    // Cần thêm EnumNotificationType.MissedCall
    public EnumNotificationType NotificationType => EnumNotificationType.SystemAnnouncement;

    public string BuildContent(MissedCallEventData data)
    {
        return $"Bạn có một cuộc gọi nhỡ từ <strong>{data.Caller.FullName}</strong>.";
    }

    public RelatedObjectInfo BuildRelatedObject(MissedCallEventData data)
    {
        // Điều hướng đến cuộc trò chuyện
        string navigateUrl = data.Conversation.ConversationType == EnumConversationType.Group
            ? $"/groups/{data.Conversation.ExplicitGroupID}/chat"
            : $"/messages/{data.Conversation.ConversationID}";

        return new() { NavigateUrl = navigateUrl };
    }
}