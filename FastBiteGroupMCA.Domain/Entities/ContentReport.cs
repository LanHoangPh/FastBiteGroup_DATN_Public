namespace FastBiteGroupMCA.Domain.Entities;

public class ContentReport : IDateTracking
{
    public int ContentReportID { get; set; }
    public int ReportedContentID { get; set; }
    public EnumReportedContentType ReportedContentType { get; set; }
    public Guid ReportedByUserID { get; set; }
    public AppUser ReportedByUser { get; set; } = null!;
    // --- BỔ SUNG THUỘC TÍNH NÀY ---
    /// <summary>
    /// ID của người dùng sở hữu nội dung bị báo cáo.
    /// </summary>
    public Guid ReportedContentOwnerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public EnumContentReportStatus Status { get; set; } = EnumContentReportStatus.Pending;
    public Guid GroupID { get; set; }
    public Group Group { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
