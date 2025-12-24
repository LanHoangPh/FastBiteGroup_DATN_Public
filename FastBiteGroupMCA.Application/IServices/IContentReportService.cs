using FastBiteGroupMCA.Application.DTOs.ContentReport;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices;

public interface IContentReportService
{
    Task<ApiResponse<object>> ReportContentAsync(Guid groupId, CreateContentReportDto dto);
}
