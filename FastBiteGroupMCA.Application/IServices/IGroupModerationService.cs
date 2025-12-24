using FastBiteGroupMCA.Application.DTOs.ContentReport;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices;

public interface IGroupModerationService
{
    Task<ApiResponse<PagedResult<GroupReportedContentDto>>> GetPendingReportsAsync(Guid groupId, GetPendingReportsQuery query);
    Task<ApiResponse<object>> TakeModerationActionAsync(Guid groupId, int reportId, Guid moderatorId, ModerationActionDto dto);
}
