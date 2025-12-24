using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record NewContentReportEventData(Group Group, ContentReport Report, AppUser Reporter);

public class NewContentReportNotificationTemplate : INotificationTemplate<NewContentReportEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.SystemAnnouncement; // Hoặc một type mới

    public string BuildContent(NewContentReportEventData data)
    {
        return $"Có một báo cáo mới từ <strong>{data.Reporter.FullName}</strong> về một {data.Report.ReportedContentType.ToString().ToLower()} trong nhóm <strong>{data.Group.GroupName}</strong>.";
    }

    public RelatedObjectInfo BuildRelatedObject(NewContentReportEventData data)
    {
        // Điều hướng Admin/Mod đến trang quản lý báo cáo của nhóm
        return new()
        {
            ObjectType = EnumNotificationObjectType.Group,
            ObjectId = data.Group.GroupID.ToString(),
            NavigateUrl = $"/admin/groups/{data.Group.GroupID}/reports/{data.Report.ContentReportID}"
        };
    }
}
