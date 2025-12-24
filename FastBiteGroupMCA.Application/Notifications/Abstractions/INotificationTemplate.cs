using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Abstractions;
/// <summary>
/// Interface đánh dấu (marker interface) cho tất cả các template thông báo,
/// giúp cho việc tự động đăng ký qua Dependency Injection.
/// </summary>
public interface INotificationTemplate
{
}
/// <summary>
/// Interface chung chứa các phương thức cần thiết cho một template thông báo.
/// </summary>
/// <typeparam name="TEventData">Kiểu dữ liệu chứa thông tin của sự kiện.</typeparam>
public interface INotificationTemplate<in TEventData> : INotificationTemplate
{
    EnumNotificationType NotificationType { get; }
    string BuildContent(TEventData data);
    RelatedObjectInfo BuildRelatedObject(TEventData data);
}
