using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.DTOs.Admin.User;
using FastBiteGroupMCA.Application.DTOs.Role;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.Response;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/admin/users")] 
[ApiController]
[Produces("application/json")]
[Authorize(Roles = "Admin")] 
[ApiExplorerSettings(GroupName = "Admin-v1")] 

public class AdminController : ControllerBase
{
    private readonly IUserService _userService; 
    private readonly IAdminUserService _adminUserService; 
    private readonly ICurrentUser _currentUser; 
    private readonly IBackgroundJobClient _backgroundJobClient; 
    private readonly IAdminExportService _adminExportService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IUserService userService, IAdminUserService adminUserService, ILogger<AdminController> logger, ICurrentUser currentUser, IAdminExportService adminExportService, IBackgroundJobClient backgroundJobClient)
    {
        _userService = userService;
        _adminUserService = adminUserService;
        _logger = logger;
        _currentUser = currentUser;
        _adminExportService = adminExportService;
        _backgroundJobClient = backgroundJobClient;
    }
    /// <summary>
    /// [Admin] Lấy thông tin hồ sơ của chính Admin đang đăng nhập.
    /// </summary>
    /// <returns></returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyAdminProfile()
    {
        _logger.LogInformation("Admin requested their own profile information.");
        var result = await _adminUserService.GetMyAdminProfileAsync();
        return result.Success ? Ok(result) : Unauthorized(result);
    }
    /// <summary>
    /// [Admin] Tìm kiếm người dùng có sẵn để mời vào nhóm.
    /// </summary>
    [HttpGet("search-available")]
    public async Task<IActionResult> SearchAvailableUsers([FromQuery] UserSearchRequest request)
    {
        _logger.LogInformation("Admin requested to search available users with parameters: {@Request}", request);
        // Gọi phương thức service MỚI
        var result = await _adminUserService.SearchAvailableUsersAsync(request);
        return Ok(result);
    }
    /// <summary>
    /// [Admin] Lấy danh sách tất cả người dùng (phân trang).
    /// </summary>
    /// <remarks>
    /// Endpoint này cho phép Admin truy xuất danh sách toàn bộ người dùng trong hệ thống với cơ chế phân trang.
    /// </remarks>
    /// <param name="request">Tham số phân trang bao gồm PageNumber và PageSize.</param>
    /// <response code="200">Trả về danh sách người dùng thành công.</response>
    /// <response code="401">Chưa xác thực.</response>
    /// <response code="403">Không có quyền Admin.</response>
    [HttpGet] // <-- Route gốc của controller /api/v1/users
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersAdminParams request)
    {
        _logger.LogInformation("Admin requested user list with parameters: {@Request}", request);
        var result = await _adminUserService.GetUsersForAdminAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Bắt đầu một tác vụ nền để xuất dữ liệu người dùng ra file Excel.
    /// </summary>
    [HttpPost("export-jobs")]
    public IActionResult CreateUserExportJob([FromBody] GetUsersAdminParams filters)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId))
            return Unauthorized();
        var adminFullName = _currentUser.FullName ?? "Admin";

        _backgroundJobClient.Enqueue<IAdminExportService>(
            service => service.GenerateUsersExportFileAsync(filters, adminId, adminFullName)
        );

        return Accepted(ApiResponse<object>.Ok(null, "Yêu cầu xuất file đã được tiếp nhận và đang được xử lý."));
    }


    /// <summary>
    /// [Admin] Lấy thông tin chi tiết tổng quan của một người dùng.
    /// </summary>
    /// <remarks>Bao gồm các thống kê (số group tham gia, số bài viết) và danh sách các nhóm người dùng này là thành viên.</remarks>
    /// <response code="200">Trả về thông tin chi tiết của người dùng.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    [HttpGet("{userId:guid}/details")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetailsAsync([FromRoute] Guid userId)
    {
        _logger.LogInformation("Admin requested details for user ID: {UserId}", userId);
        var result = await _adminUserService.GetUserDetailForAdminAsync(userId); // Giả sử service được inject
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// [Admin] Lấy lịch sử hoạt động của một người dùng.
    /// </summary>
    /// <remarks>Hỗ trợ lọc theo loại hoạt động (Post, Comment) và lọc theo group.</remarks>
    /// <response code="200">Trả về danh sách hoạt động.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    [HttpGet("{userId:guid}/activity")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserActivityDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserActivityAsync([FromRoute] Guid userId, [FromQuery] GetUserActivityParams request)
    {
        _logger.LogInformation("Admin requested activity for user ID: {UserId} with parameters: {@Request}", userId, request);
        var result = await _adminUserService.GetUserActivityForAdminAsync(userId, request);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Vô hiệu hóa (khóa) tài khoản người dùng.
    /// </summary>
    /// <remarks>Người dùng bị khóa sẽ không thể đăng nhập vào hệ thống.</remarks>
    /// <response code="200">Khóa tài khoản thành công.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    [HttpPost("{userId:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUserAsync([FromRoute] Guid userId, [FromBody] DeactivateUserRequestDto request)
    {
        _logger.LogInformation("Admin requested to deactivate user ID: {UserId}", userId);
        var fullReason = $"{request.ReasonCategory}: {request.ReasonDetails ?? "N/A"}";
        var (adminID, adminFullName) = GetAdminInfo();
        var result = await _adminUserService.DeactivateUserAccountAsync(userId, request.RowVersion, adminID, adminFullName, fullReason);
        if (!result.Success)
        {
            // Kiểm tra mã lỗi để trả về status code phù hợp
            if (result.Errors?.Any(e => e.ErrorCode == "CONCURRENCY_ERROR") ?? false)
                return Conflict(result); // 409 Conflict là response chuẩn cho lỗi tương tranh

            if (result.Errors?.Any(e => e.ErrorCode == "USER_NOT_FOUND") ?? false)
                return NotFound(result);

            return BadRequest(result);
        }
        return result.Success ? Ok(result) : NotFound(result); // Service nên trả về not found nếu user không tồn tại
    }

    /// <summary>
    /// [Admin] Kích hoạt lại tài khoản người dùng đã bị khóa.
    /// </summary>
    /// <response code="200">Kích hoạt tài khoản thành công.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    [HttpPost("{userId:guid}/reactivate")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateUserAsync([FromRoute] Guid userId, UserActionRequest request)
    {
        _logger.LogInformation("Admin requested to reactivate user ID: {UserId}", userId);
        var (adminID, adminFullName) = GetAdminInfo();
        var result = await _adminUserService.ReactivateUserAccountAsync(userId, request.RowVersion, adminID, adminFullName);
        if (!result.Success)
        {
            // Kiểm tra mã lỗi để trả về status code phù hợp
            if (result.Errors?.Any(e => e.ErrorCode == "CONCURRENCY_ERROR") ?? false)
                return Conflict(result); // 409 Conflict là response chuẩn cho lỗi tương tranh

            if (result.Errors?.Any(e => e.ErrorCode == "USER_NOT_FOUND") ?? false)
                return NotFound(result);

            return BadRequest(result);
        }
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// [Admin] Buộc người dùng phải đổi mật khẩu trong lần đăng nhập tiếp theo.
    /// </summary>
    /// <response code="200">Yêu cầu đặt lại mật khẩu thành công.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    [HttpPost("{userId:guid}/force-password-reset")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForcePasswordResetAsync([FromRoute] Guid userId)
    {
        _logger.LogInformation("Admin requested password reset for user ID: {UserId}", userId);
        var result = await _adminUserService.ForcePasswordResetAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }
    /// <summary>
    /// [Admin] Tạo một người dùng mới.
    /// </summary>
    /// <param name="createUserDto">Thông tin chi tiết của người dùng cần tạo.</param>
    /// <response code="201">Tạo người dùng thành công. Trả về thông tin người dùng đã tạo.</response>
    /// <response code="400">Dữ liệu đầu vào không hợp lệ (ví dụ: email trùng, thiếu trường).</response>
    /// <response code="401">Chưa xác thực.</response>
    /// <response code="403">Không có quyền Admin.</response>
    [HttpPost] // <-- Route: POST /api/v1/users
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserByAdminRequest createUserDto)
    {
        _logger.LogInformation("Admin requested to create a new user with data: {@CreateUserDto}", createUserDto);
        var result = await _adminUserService.CreateUserAsAdminAsync(createUserDto);
        return result.Success
            ? StatusCode(StatusCodes.Status201Created, result)
            : BadRequest(result);
    }
    /// <summary>
    /// [Admin] Cập nhật thông tin người dùng.
    /// </summary>
    /// <remarks>
    /// Hỗ trợ upload avatar mới thông qua multipart/form-data.
    /// </remarks>
    /// <param name="userId">ID của người dùng cần cập nhật.</param>
    /// <param name="request">Thông tin cần cập nhật.</param>
    /// <response code="200">Cập nhật thành công.</response>
    /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    /// <response code="401">Chưa xác thực.</response>
    /// <response code="403">Không có quyền Admin.</response>
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [HttpPut("{userId:guid}")]// <-- Route: PUT /api/v1/users/{id}
    public async Task<IActionResult> UpdateUserBasicInfo(Guid userId, [FromBody] UpdateUserBasicInfoRequest request)
    {
        _logger.LogInformation("Admin requested to update user ID: {UserId} with data: {@Request}", userId, request);
        var result = await _adminUserService.UpdateUserBasicInfoAsync(userId, request);
        if (!result.Success)
        {
            // Kiểm tra mã lỗi để trả về status code phù hợp
            if (result.Errors?.Any(e => e.ErrorCode == "CONCURRENCY_ERROR") ?? false)
                return Conflict(result); // 409 Conflict là response chuẩn cho lỗi tương tranh

            if (result.Errors?.Any(e => e.ErrorCode == "USER_NOT_FOUND") ?? false)
                return NotFound(result);

            return BadRequest(result);
        }
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// [Admin] Xóa ảnh đại diện của một người dùng.
    /// </summary>
    [HttpDelete("{userId:guid}/avatar")]
    public async Task<IActionResult> RemoveUserAvatar(Guid userId)
    {
        _logger.LogInformation("Admin requested to remove avatar for user ID: {UserId}", userId);
        var result = await _adminUserService.RemoveUserAvatarAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// [Admin] Xóa tiểu sử (bio) của một người dùng.
    /// </summary>
    [HttpDelete("{userId:guid}/bio")]
    public async Task<IActionResult> RemoveUserBio(Guid userId)
    {
        _logger.LogInformation("Admin requested to remove bio for user ID: {UserId}", userId);
        var result = await _adminUserService.RemoveUserBioAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// [Admin] Xóa một người dùng (Soft Delete).
    /// </summary>
    /// <param name="id">ID của người dùng cần xóa.</param>
    /// <param name="rowVersion"></param>
    /// <response code="204">Xóa người dùng thành công. Không có nội dung trả về.</response>
    /// <response code="404">Không tìm thấy người dùng.</response>
    /// <response code="401">Chưa xác thực.</response>
    /// <response code="403">Không có quyền Admin.</response>
    [HttpDelete("{id:guid}")] 
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] Guid id, [FromQuery] byte[] rowVersion)
    {
        _logger.LogInformation("Admin requested to delete user ID: {Id}", id);
        var (adminID, adminFullName) = GetAdminInfo();
        var result = await _adminUserService.DeleteUserAsync(id, rowVersion, adminID, adminFullName);
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.ErrorCode == "CONCURRENCY_ERROR") ?? false)
                return Conflict(result);

            if (result.Errors?.Any(e => e.ErrorCode == "USER_NOT_FOUND") ?? false)
                return NotFound(result);

            return BadRequest(result);
        }
        return result.Success ? NoContent() : NotFound(result);
    }
    /// <summary>
    /// [Admin] Khôi phục một người dùng đã bị xóa mềm.
    /// </summary>
    [HttpPatch("{userId:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RestoreUser([FromRoute] Guid userId, [FromBody] UserActionRequest request)
    {
        _logger.LogInformation("Admin requested to restore user ID: {UserId}", userId);
        var (adminID, adminFullName) = GetAdminInfo();
        var result = await _adminUserService.RestoreUserAsync(userId, request.RowVersion, adminID, adminFullName);
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.ErrorCode == "CONCURRENCY_ERROR") ?? false)
                return Conflict(result); 

            if (result.Errors?.Any(e => e.ErrorCode == "USER_NOT_FOUND") ?? false)
                return NotFound(result);

            return BadRequest(result);
        }
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// [Admin] Gán một quyền (role) cho người dùng.
    /// </summary>
    /// <param name="id">ID của người dùng.</param>
    /// <param name="roleDto">Tên của quyền cần gán (ví dụ: "Moderator").</param>
    /// <response code="200">Gán quyền thành công.</response>
    /// <response code="400">Tên quyền không hợp lệ hoặc người dùng đã có quyền này.</response>
    /// <response code="404">Không tìm thấy người dùng hoặc quyền.</response>
    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoleAsync([FromRoute] Guid id, [FromBody] RoleAssignmentDto roleDto)
    {
        _logger.LogInformation("Admin requested to assign role '{RoleName}' to user ID: {UserId}", roleDto.RoleName, id);
        var (adminID, adminFullName) = GetAdminInfo();
        var result = await _adminUserService.AssignRoleAsync(id, roleDto.RowVersion, adminID, adminFullName, roleDto.RoleName);
        if (!result.Success)
        {
            // Kiểm tra mã lỗi để trả về status code phù hợp
            if (result.Errors?.Any(e => e.ErrorCode == "CONCURRENCY_ERROR") ?? false)
                return Conflict(result); // 409 Conflict là response chuẩn cho lỗi tương tranh

            if (result.Errors?.Any(e => e.ErrorCode == "USER_NOT_FOUND") ?? false)
                return NotFound(result);

            return BadRequest(result);
        }
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// [Admin] Xóa một quyền (role) khỏi người dùng.
    /// </summary>
    /// <param name="id">ID của người dùng.</param>
    /// <param name="roleName">Tên của quyền cần xóa.</param>
    /// <param name="rowVersion"></param>
    /// <response code="204">Xóa quyền thành công.</response>
    /// <response code="400">Người dùng không có quyền này.</response>
    /// <response code="404">Không tìm thấy người dùng hoặc quyền.</response>
    [HttpDelete("{id:guid}/roles/{roleName}")] // <-- Route: DELETE /api/v1/users/{id}/roles/{roleName}
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRoleAsync([FromRoute] Guid id, [FromRoute] string roleName, [FromQuery] byte[] rowVersion)
    {
        _logger.LogInformation("Admin requested to remove role '{RoleName}' from user ID: {UserId}", roleName, id);
        var result = await _adminUserService.RemoveRoleAsync(id, rowVersion, roleName);
        if (!result.Success)
        {
            // Kiểm tra mã lỗi để trả về status code phù hợp
            if (result.Errors?.Any(e => e.ErrorCode == "CONCURRENCY_ERROR") ?? false)
                return Conflict(result); // 409 Conflict là response chuẩn cho lỗi tương tranh

            if (result.Errors?.Any(e => e.ErrorCode == "USER_NOT_FOUND") ?? false)
                return NotFound(result);

            return BadRequest(result);
        }
        return result.Success ? NoContent() : BadRequest(result);
    }

    /// <summary>
    /// [Admin] Thực hiện một hành động hàng loạt trên nhiều người dùng.
    /// </summary>
    [HttpPost("bulk-action")]
    public IActionResult PerformBulkUserAction([FromBody] BulkUserActionRequest request)
    {
        _logger.LogInformation("Admin requested bulk user action with parameters: {@Request}", request);
        var result = _adminUserService.PerformBulkUserActionAsync(request);
        return Ok(result); // Luôn trả về 200 OK vì đây là tác vụ nền
    }
    /// <summary>
    /// [Admin] Cập nhật/Thay đổi vai trò chính của người dùng.
    /// </summary>
    /// <remarks>
    /// Hành động này sẽ xóa tất cả các vai trò hiện tại của người dùng và gán một vai trò mới được chỉ định bởi RoleId.
    /// </remarks>
    /// <param name="userId">ID của người dùng cần thay đổi vai trò.</param>
    /// <param name="dto">Đối tượng chứa ID của vai trò mới.</param>
    /// <response code="200">Cập nhật vai trò thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc không thể thay đổi vai trò.</response>
    /// <response code="404">Không tìm thấy người dùng hoặc vai trò.</response>
    [HttpPut("{userId:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoleAsync([FromRoute] Guid userId, [FromBody] UpdateUserRoleDto dto)
    {
        _logger.LogInformation("Admin requested to change role for user ID: {UserId} to new Role ID: {NewRoleId}", userId, dto.NewRoleId);
        var result = await _userService.ChangeUserRoleAsync(userId, dto.NewRoleId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private (Guid,string) GetAdminInfo()
    {
        if (!Guid.TryParse(_currentUser.Id, out Guid adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            throw new UnauthorizedAccessException("Invalid admin user context.");
        }
        var adminFullName = _currentUser.FullName;
        return (adminId, adminFullName);
    }
}

