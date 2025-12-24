using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

/// <summary>
/// Dữ liệu cần thiết để tạo thông báo bị xóa khỏi nhóm.
/// </summary>
public record UserKickedEventData(Group Group, AppUser KickedByUser);

/// <summary>
/// Template để xây dựng nội dung thông báo khi một người dùng bị xóa khỏi nhóm.
/// </summary>
public class UserKickedNotificationTemplate : INotificationTemplate<UserKickedEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.UserKickedFromGroup;

    public string BuildContent(UserKickedEventData data)
    {
        return $"Bạn đã bị xóa khỏi nhóm '{data.Group.GroupName}' bởi {data.KickedByUser.FullName}.";
    }

    public RelatedObjectInfo BuildRelatedObject(UserKickedEventData data)
    {
        // QUAN TRỌNG: Không có link điều hướng cụ thể vì người dùng không còn quyền truy cập vào nhóm.
        // Trả về null để frontend không hiển thị đây là một thông báo có thể nhấp vào.
        return null!;
    }
}
