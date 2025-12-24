namespace FastBiteGroupMCA.Application.DTOs.ContentReport;

public class GroupReportedContentDto
{
    public int ReportId { get; set; }
    public int ContentId { get; set; }
    public string ContentType { get; set; } = null!;
    public string ContentPreview { get; set; } = null!;
    public string AuthorName { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
}
