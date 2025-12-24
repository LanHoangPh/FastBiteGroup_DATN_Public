using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.ContentReport;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/groups/{groupId:guid}/reports")]
[Produces("application/json")]
[Authorize]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class GroupReportsController : ControllerBase
{
    private readonly IContentReportService _contentReportService;
    private readonly IGroupModerationService _moderationService;
    private readonly ICurrentUser _currentUser;

    public GroupReportsController(IContentReportService contentReportService, IGroupModerationService moderationService, ICurrentUser currentUser)
    {
        _contentReportService = contentReportService;
        _moderationService = moderationService;
        _currentUser = currentUser;  
    }

    /// <summary>
    /// Gửi một báo cáo vi phạm về bài viết hoặc bình luận.
    /// </summary>
    /// <remarks>Bất kỳ thành viên nào của nhóm cũng có thể gửi báo cáo.</remarks>
    /// <param name="groupId">ID của nhóm chứa nội dung bị báo cáo.</param>
    /// <param name="dto">Thông tin chi tiết về báo cáo.</param>
    /// <response code="201">Gửi báo cáo thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc không thể báo cáo nội dung này.</response>
    /// <response code="403">Không phải là thành viên của nhóm.</response>
    [HttpPost]
    [Tags("Groups.Moderation")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReportContent(Guid groupId, [FromBody] CreateContentReportDto dto)
    {
        var result = await _contentReportService.ReportContentAsync(groupId, dto);

        if (!result.Success)
        {
            return result.Errors?.FirstOrDefault()?.ErrorCode switch
            {
                "GROUP_NOT_FOUND" => NotFound(result),
                "CONTENT_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => Forbid(),
                "ALREADY_REPORTED" => Conflict(result),
                _ => BadRequest(result)
            };
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }
    /// <summary>
    /// (Kiểm duyệt viên) Lấy danh sách các báo cáo đang chờ xử lý.
    /// </summary>
    /// <remarks>Chỉ người có quyền kiểm duyệt (Moderator hoặc Admin của nhóm) mới có thể truy cập.</remarks>
    /// <param name="groupId">ID của nhóm.</param>
    /// <param name="query">Thông tin phân trang.</param>
    /// <response code="200">Trả về danh sách các báo cáo đang chờ.</response>
    /// <response code="403">Không có quyền kiểm duyệt trong nhóm này.</response>
    [HttpGet]
    [Tags("Groups.Moderation")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<GroupReportedContentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingReportsAsync(Guid groupId, [FromQuery] GetPendingReportsQuery query)
    {
        // Controller giờ rất gọn, chỉ chuyển tiếp request
        var result = await _moderationService.GetPendingReportsAsync(groupId, query);

        if (!result.Success)
        {
            return result.Errors?.FirstOrDefault()?.ErrorCode switch
            {
                "FORBIDDEN" => Forbid(),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// (Kiểm duyệt viên) Thực hiện hành động đối với một báo cáo.
    /// </summary>
    /// <remarks>
    /// Các hành động có thể là:
    /// - `DISMISS_REPORT`: Bỏ qua báo cáo, giữ lại nội dung.
    /// - `REMOVE_CONTENT`: Xóa nội dung bị báo cáo.
    /// - `REMOVE_CONTENT_AND_WARN_USER`: Xóa nội dung và cảnh cáo người dùng.
    /// - `REMOVE_CONTENT_AND_BAN_USER`: Xóa nội dung và cấm người dùng khỏi nhóm.
    /// </remarks>
    /// <param name="groupId">ID của nhóm.</param>
    /// <param name="reportId">ID của báo cáo cần xử lý.</param>
    /// <param name="dto">Thông tin về hành động kiểm duyệt.</param>
    /// <response code="204">Xử lý báo cáo thành công.</response>
    /// <response code="403">Không có quyền kiểm duyệt.</response>
    /// <response code="404">Không tìm thấy báo cáo.</response>
    [HttpPost("{reportId:int}/action")]
    [Tags("Groups.Moderation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TakeModerationAction(Guid groupId, int reportId, [FromBody] ModerationActionDto dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return Unauthorized();
        var response = await _moderationService.TakeModerationActionAsync(groupId, reportId, userId, dto);

        if (!response.Success)
        {
            return response.Errors?.FirstOrDefault()?.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(response),
                "FORBIDDEN" => Forbid(),
                _ => BadRequest(response)
            };
        }
        return NoContent(); // Dùng 204 No Content cho hành động thành công
    }
}
