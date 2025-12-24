using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record AccountDeactivatedEventData(string Reason);

public class AccountDeactivatedNotificationTemplate : INotificationTemplate<AccountDeactivatedEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.AccountDeactivated;

    public string BuildContent(AccountDeactivatedEventData data)
    {
        return $"Tài khoản của bạn đã bị tạm khóa. Lý do: {data.Reason}";
    }

    public RelatedObjectInfo BuildRelatedObject(AccountDeactivatedEventData data)
    {
        return new()
        {
            ObjectType = EnumNotificationObjectType.ExternalLink,
            NavigateUrl = "/support/contact"
        };
    }
}
