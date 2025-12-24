using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.Notification;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(INotificationService notificationService, ICurrentUser currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }
    /// <summary>
    /// Lấy danh sách thông báo của người dùng hiện tại (phân trang).
    /// </summary>
    /// <param name="pageParams">Các tham số để phân trang và lọc.</param>
    /// <response code="200">Trả về danh sách thông báo thành công.</response>
    /// <response code="401">Nếu người dùng chưa xác thực.</response>
    [HttpGet("me")]
    [Tags("Users.Notifications")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NotificationDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] GetMyNotification pageParams)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
        {
            return BadRequest("Invalid user ID.");
        }

        var notifications = await _notificationService.GetMyNotificationsAsync(userId, pageParams);
        return Ok(notifications);
    }
    /// <summary>
    /// Đánh dấu một thông báo cụ thể là đã đọc.
    /// </summary>
    /// <param name="id">ID (chuỗi) của thông báo từ MongoDB.</param>
    /// <response code="204">Đánh dấu đã đọc thành công.</response>
    /// <response code="401">Nếu người dùng chưa xác thực.</response>
    /// <response code="404">Nếu không tìm thấy thông báo hoặc không có quyền truy cập.</response>
    [HttpPost("{id}/mark-as-read")]
    [Tags("Users.Notifications")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("INVALID_TOKEN", "User ID trong token không hợp lệ."));
        }

        var result = await _notificationService.MarkAsReadAsync(id, userId);

        if (!result.Success && result.Errors?.FirstOrDefault()?.ErrorCode == "NOTIFICATION_NOT_FOUND")
        {
            return NotFound(result);
        }

        return result.Success ? NoContent() : BadRequest(result);
    }


    /// <summary>
    /// Đánh dấu tất cả thông báo chưa đọc của người dùng là đã đọc.
    /// </summary>
    /// <response code="204">Đánh dấu tất cả đã đọc thành công.</response>
    /// <response code="401">Nếu người dùng chưa xác thực.</response>
    [HttpPost("me/mark-all-as-read")]
    [Tags("Users.Notifications")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("INVALID_TOKEN", "User ID trong token không hợp lệ."));
        }
        await _notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }
}

