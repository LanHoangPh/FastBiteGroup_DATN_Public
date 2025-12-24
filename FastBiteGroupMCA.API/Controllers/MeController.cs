using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/me")]
[Produces("application/json")]
[Authorize]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class MeController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IUserService _userService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MeController> _logger;

    public MeController(IGroupService groupService, ICurrentUser currentUser, IUserService userService, ILogger<MeController> logger)
    {
        _groupService = groupService;
        _currentUser = currentUser;
        _userService = userService;
        _logger = logger;
    }
    /// <summary>
    /// Lấy thông tin người dùng hiện tại (đã xác thực).
    /// </summary>
    /// <returns></returns>
    [HttpGet("profile")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> GetMyProfileAsync()
    {
        var result = await _userService.GetMyProfileAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Cập nhật thông tin người dùng hiện tại (đã xác thực).
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("profile")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> UpdateProfileInfo([FromBody] UpdateUserADDto request)
    {
        var result = await _userService.UpdateProfileInfoAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Lấy danh sách các nhóm mà tôi đã tham gia hoặc tạo ra.
    /// </summary>
    /// <remarks>
    /// Trả về một danh sách các nhóm mà người dùng hiện tại là thành viên, có hỗ trợ phân trang.
    /// </remarks>
    /// <param name="query">Các tham số phân trang (pageNumber, pageSize).</param>
    [HttpGet("groups")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> GetMyAssociatedGroups([FromQuery] GetUserGroupsQuery query)
    {
        var result = await _groupService.GetUserAssociatedGroupsAsync(query);

        return Ok(result);
    }

    /// <summary>
    /// Cập nhật ảnh đại diện cho người dùng đang đăng nhập.
    /// </summary>
    [HttpPut("avatar")] // Dùng PUT để cập nhật sẽ chuẩn hơn POST
    [Tags("Current User (Me)")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateMyAvatar(IFormFile file)
    {
        if (file == null)
            return BadRequest("File is required.");

        var result = await _userService.UpdateUserAvatarAsync(file);

        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Thay đổi mật khẩu cho người dùng đang đăng nhập.
    /// </summary>
    [HttpPut("password")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO dto)
    {
        var result = await _userService.ChangePasswordAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Lấy 10 lần đăng nhập gần nhất của người dùng hiện tại.
    /// </summary>
    [HttpGet("login-history")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> GetMyLoginHistory()
    {
        // Logic này nên được đặt trong UserService
        var result = await _userService.GetMyLoginHistoryAsync();
        return Ok(result);
    }
    /// <summary>
    /// Yêu cầu xóa tài khoản người dùng hiện tại. Yêu cầu mật khẩu hiện tại để xác nhận.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("delete-request")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> RequestAccountDeletion([FromBody] DeleteAccountRequestDto dto)
    {
        var result = await _userService.RequestAccountDeletionAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Yêu cầu hủy kích hoạt tài khoản. Yêu cầu mật khẩu hiện tại để xác nhận.
    /// </summary>
    [HttpPost("deactivate")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> DeactivateAccount([FromBody] DeactivateAccountDto dto)
    {
        var result = await _userService.DeactivateAccountAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Cập nhật cài đặt quyền riêng tư cho người dùng hiện tại.
    /// </summary>
    [HttpPut("settings/privacy")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> UpdatePrivacySettings([FromBody] UpdatePrivacySettingsDto dto)
    {
        var result = await _userService.UpdatePrivacySettingsAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Lấy danh bạ của người dùng hiện tại.
    /// </summary>
    /// <returns></returns>
    [HttpGet("contacts")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> GetMyContacts()
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return Unauthorized();

        var result = await _userService.GetMyContactsAsync(currentUserId);
        return Ok(result);
    }
    /// <summary>
    /// Đăng ký hoặc cập nhật OneSignal PlayerId cho người dùng hiện tại.
    /// </summary>
    [HttpPost("notifications/subscribe")]
    [Tags("Current User (Me)")]
    public async Task<IActionResult> SubscribeNotifications([FromBody] SubscribeNotificationRequest request)
    {
        _logger.LogInformation("SubscribeNotifications called for user {UserId} with PlayerId {PlayerId}", _currentUser.Id, request.PlayerId);
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ."));

        var result = await _userService.SubscribeToPushNotificationsAsync(userId, request.PlayerId);

        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Lấy các số liệu thống kê nhanh cho dashboard của người dùng hiện tại.
    /// </summary>
    [HttpGet("dashboard-stats")]
    [Tags("Current User (Me)")]
    [ProducesResponseType(typeof(ApiResponse<UserDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _userService.GetDashboardStatsAsync();
        return Ok(result);
    }
}
