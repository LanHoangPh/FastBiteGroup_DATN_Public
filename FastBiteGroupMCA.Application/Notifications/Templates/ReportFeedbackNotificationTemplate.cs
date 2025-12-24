using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record ReportFeedbackEventData(ContentReport Report, Group Group);

public class ReportFeedbackNotificationTemplate : INotificationTemplate<ReportFeedbackEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.SystemAnnouncement;

    public string BuildContent(ReportFeedbackEventData data)
    {
        string statusText = data.Report.Status == EnumContentReportStatus.Reviewed ? "đã được xử lý" : "đã được xem xét và không có vi phạm";
        return $"Cảm ơn bạn đã báo cáo. Báo cáo của bạn về một nội dung trong nhóm '{data.Group.GroupName}' {statusText}.";
    }

    public RelatedObjectInfo BuildRelatedObject(ReportFeedbackEventData data) => null;
}
