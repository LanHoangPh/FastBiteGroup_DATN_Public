using FastBiteGroupMCA.Application.DTOs.Admin.AdminNotifications;
using FastBiteGroupMCA.Application.DTOs.Notification;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/admin/notification")]
[ApiController]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin-v1")]
public class AdminNotificationController : ControllerBase
{
    private readonly IAdminNotificationService _notificationService;
    public AdminNotificationController(IAdminNotificationService svc) => _notificationService = svc;

    /// <summary>
    /// [Admin] Gửi một thông báo đến tất cả người dùng.
    /// </summary>
    /// <remarks>
    /// API này tiếp nhận yêu cầu tạo thông báo và đưa vào hàng đợi (queue).
    /// Một tiến trình nền (background worker) sẽ chịu trách nhiệm gửi thông báo đến người dùng.
    /// Do đó, API sẽ trả về mã 202 Accepted ngay lập tức để xác nhận yêu cầu đã được tiếp nhận.
    /// </remarks>
    /// <param name="requestDto">Nội dung và URL điều hướng của thông báo.</param>
    /// <response code="202">Yêu cầu đã được chấp nhận và đưa vào hàng đợi để xử lý.</response>
    /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
    /// <response code="401">Chưa xác thực.</response>
    /// <response code="403">Không có quyền Admin.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequestDTO requestDto)
    {
        var result = await _notificationService.EnqueueBroadcastAsync(requestDto);
        return result.Success ? Accepted(result) : BadRequest(result);
    }

    /// <summary>
    /// [Admin] Lấy danh sách thông báo đã gửi (phân trang).
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] GetAdminNotificationsParams request)
    {
        var result = await _notificationService.GetNotificationsAsync(request);
        return Ok(result);
    }
    /// <summary>
    /// Đánh dấu một thông báo cụ thể là đã đọc.
    /// </summary>
    /// <param name="notificationId"></param>
    /// <returns></returns>
    [HttpPost("{notificationId:long}/mark-as-read")]
    public async Task<IActionResult> MarkNotificationAsRead(long notificationId)
    {
        var result = await _notificationService.MarkAsReadAsync(notificationId);
        return result.Success ? Ok(result) : NotFound(result);
    }
    /// <summary>
    /// Đánh dấu tất cả thông báo chưa đọc là đã đọc.
    /// </summary>
    /// <returns></returns>
    [HttpPost("mark-all-as-read")]
    public async Task<IActionResult> MarkAllNotificationsAsRead()
    {
        var result = await _notificationService.MarkAllAsReadAsync();
        return Ok(result);
    }
}
