using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.ContentReport;

public class CreateContentReportDto
{
    public int ContentId { get; set; }

    public EnumReportedContentType ContentType { get; set; }  // đổi từ string sang enum

    public string Reason { get; set; } = string.Empty;
}
