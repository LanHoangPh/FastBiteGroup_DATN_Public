using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record ActionTakenOnContentEventData(ContentReport Report, Group Group, EnumModerationAction Action);

public class ActionTakenOnContentNotificationTemplate : INotificationTemplate<ActionTakenOnContentEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.SystemAnnouncement;

    public string BuildContent(ActionTakenOnContentEventData data)
    {
        string actionText = data.Action switch
        {
            EnumModerationAction.RemoveContent => "đã bị xóa",
            EnumModerationAction.RemoveContentAndWarnUser => "đã bị xóa và bạn nhận được một cảnh cáo",
            EnumModerationAction.RemoveContentAndBanUser => "đã bị xóa và bạn đã bị cấm khỏi nhóm",
            _ => "đã được xem xét"
        };
        return $"Nội dung của bạn trong nhóm '{data.Group.GroupName}' {actionText} do vi phạm chính sách cộng đồng.";
    }

    public RelatedObjectInfo BuildRelatedObject(ActionTakenOnContentEventData data)
    {
        return new() { NavigateUrl = $"/community-guidelines" };
    }
}
