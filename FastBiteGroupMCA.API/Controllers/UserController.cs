using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[Authorize] // Bảo vệ controller này, chỉ cho phép người dùng đã xác thực truy cập
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserPresenceService _presenceService;

    public UserController(IUserService userService, IUserPresenceService presenceService)
    {
        _userService = userService;
        _presenceService = presenceService;
    }
    /// <summary>
    /// Tìm kiếm người dùng trong hệ thống.
    /// </summary>
    /// <remarks>Dùng để tìm kiếm người dùng để bắt đầu cuộc trò chuyện hoặc mời vào nhóm.</remarks>
    /// <param name="request">Đối tượng chứa từ khóa tìm kiếm và các bộ lọc khác.</param>
    [HttpGet("search")]
    [Tags("Users.General")]
    public async Task<IActionResult> SearchUsersAsync([FromQuery] UserSearchForInviteRequest request)
    {
        var result = await _userService.SearchUsersForInviteAsync(request);
        return Ok(result);
    }
    /// <summary>
    /// Lấy danh sách các nhóm chung giữa người dùng hiện tại và một người dùng khác.
    /// </summary>
    /// <param name="userId">ID của người dùng khác.</param>
    [HttpGet("{userId:guid}/mutual-groups")]
    [Tags("Users.General")]
    [ProducesResponseType(typeof(ApiResponse<List<MutualGroupDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMutualGroups(Guid userId)
    {
        var result = await _userService.GetMutualGroupsAsync(userId);
        return Ok(result);
    }
    /// <summary>
    /// Lấy trạng thái trực tuyến của nhiều người dùng.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("presence/statuses")]
    [Tags("Users.General")]
    public async Task<IActionResult> GetUserStatuses([FromBody] GetUserStatusesRequest request)
    {
        var statuses = await _presenceService.GetStatusesForUsersAsync(request.UserIds);

        var resultDto = statuses.Select(kvp => new UserStatusDto { UserId = kvp.Key, Status = kvp.Value }).ToList();

        return Ok(ApiResponse<List<UserStatusDto>>.Ok(resultDto));
    }
    /// <summary>
    /// Tìm kiếm người dùng để mời vào một nhóm.
    /// </summary>
    /// <remarks>
    /// Kết quả sẽ ưu tiên những người đã có liên quan (chung nhóm, đã chat 1-1)
    /// và loại trừ những người đã là thành viên của nhóm được chỉ định.
    /// </remarks>
    /// <param name="request">Chứa GroupId để loại trừ và từ khóa tìm kiếm.</param>
    [HttpGet("search-for-invite")]
    [Tags("Users.General")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserSearchResultDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsersForInvite([FromQuery] UserSearchForInviteRequest request)
    {
        var result = await _userService.SearchUsersForInviteAsync(request);
        return Ok(result);
    }
}
