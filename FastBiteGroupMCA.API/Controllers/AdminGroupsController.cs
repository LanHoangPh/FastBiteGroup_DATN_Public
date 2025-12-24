using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.Admin.Group;
using FastBiteGroupMCA.Application.DTOs.Admin.User;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/admin/groups")]
[ApiController]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin-v1")]
public class AdminGroupsController : ControllerBase
{
    private readonly IAdminGroupService _adminGroupService;
    private readonly ICurrentUser _currentUser;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<AdminGroupsController> _logger;

    public AdminGroupsController(IAdminGroupService adminGroupService, ILogger<AdminGroupsController> logger, ICurrentUser currentUser, IAdminExportService adminExportService, IBackgroundJobClient backgroundJobClient)
    {
        _adminGroupService = adminGroupService;
        _logger = logger;
        _currentUser = currentUser;
        _backgroundJobClient = backgroundJobClient;
    }
    /// <summary>
    /// [Admin] Lấy danh sách tất cả các nhóm.
    /// </summary>
    /// <remarks>Hỗ trợ tìm kiếm, lọc theo loại nhóm, trạng thái lưu trữ và có phân trang.</remarks>
    /// <response code="200">Trả về danh sách các nhóm thành công.</response>
    [ProducesResponseType(typeof(ApiResponse<PagedResult<GroupForList_AdminDto>>), StatusCodes.Status200OK)]
    [HttpGet] // get: api/v1/admin/groups
    public async Task<IActionResult> GetGroups([FromQuery] GetGroupsAdminParams request)
    {
        _logger.LogInformation("Fetching groups for admin with parameters: {@Request}", request);
        var result = await _adminGroupService.GetGroupsForAdminAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// [Admin] Bắt đầu một tác vụ nền để xuất dữ liệu người dùng ra file Excel.
    /// </summary>
    [HttpPost("export-jobs")]
    public IActionResult CreateGroupExportJob([FromBody] GetGroupsAdminParams filters)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId))
            return Unauthorized();
        var adminFullName = _currentUser.FullName ?? "Admin";

        _backgroundJobClient.Enqueue<IAdminExportService>(
            service => service.GenerateGroupsExportFileAsync(filters, adminId, adminFullName)
        );

        return Accepted(ApiResponse<object>.Ok(null, "Yêu cầu xuất file đã được tiếp nhận và đang được xử lý."));
    }
    /// <summary>
    /// [Admin] Tạo một nhóm mới và chỉ định quản trị viên ban đầu.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupAsAdminRequest request)
    {
        var result = await _adminGroupService.CreateGroupAsAdminAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }
        return CreatedAtAction(nameof(GetGroupDetails), new { groupId = result.Data!.GroupId }, result);
    }

    /// <summary>
    /// (Admin) Tạo một nhóm trò chuyện mới.
    /// </summary>
    [HttpPost("chat")]
    [Tags("Admin.Groups")]
    public async Task<IActionResult> CreateChatGroup([FromBody] CreateChatGroupAsAdminDto dto)
    {
        var response = await _adminGroupService.CreateChatGroupAsAdminAsync(dto);
        if (!response.Success) return BadRequest(response);
        return StatusCode(201, response);
    }

    /// <summary>
    /// (Admin) Tạo một nhóm cộng đồng mới.
    /// </summary>
    [HttpPost("community")]
    [Tags("Admin.Groups")]
    public async Task<IActionResult> CreateCommunityGroup([FromBody] CreateCommunityGroupAsAdminDto dto)
    {
        var response = await _adminGroupService.CreateCommunityGroupAsAdminAsync(dto);
        if (!response.Success) return BadRequest(response);
        return StatusCode(201, response);
    }
    /// <summary>
    /// [Admin] Xóa (mềm) một nhóm.
    /// </summary>
    /// <response code="204">Xóa nhóm thành công.</response>
    /// <response code="404">Không tìm thấy nhóm.</response>
    [HttpDelete("{groupId:guid}")] 
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup([FromRoute] Guid groupId)
    {
        _logger.LogInformation("Admin is attempting to soft delete group with ID: {GroupId}", groupId);

        var (adminId, adminFullName) = GetAdminInfo();
        var result = await _adminGroupService.SoftDeleteGroupAsync(groupId, adminId, adminFullName);
        if (result.Success) return Ok(result);
        return result.Errors!.First().ErrorCode == "GROUP_NOT_FOUND"
               ? NotFound(result)
               : BadRequest(result);
    }
    /// <summary>
    /// [Admin tổng] Khôi phục một nhóm đã bị xóa mềm.
    /// </summary>
    [HttpPatch("{groupId}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestoreGroup(Guid groupId)
    {
        _logger.LogInformation("Admin is attempting to restore group with ID: {GroupId}", groupId);

        var (adminId, adminFullName) = GetAdminInfo();
        var result = await _adminGroupService.RestoreGroupAsAdminAsync(groupId, adminId, adminFullName);

        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.ErrorCode.Contains("NOT_FOUND")) ?? false)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
    /// <summary>
    /// [Admin tổng] Cập nhật ảnh đại diện cho một nhóm bất kỳ.
    /// </summary>
    [HttpPost("{groupId}/avatar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateGroupAvatar(Guid groupId, IFormFile file)
    {
        _logger.LogInformation("Admin is attempting to update avatar for group with ID: {GroupId}", groupId);
        var result = await _adminGroupService.UpdateGroupAvatarAsAdminAsync(groupId, file);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
    /// <summary>
    /// [Admin] Cập nhật thông tin cơ bản của nhóm.
    /// </summary>
    /// <response code="200">Cập nhật thành công.</response>
    /// <response code="404">Không tìm thấy nhóm.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    [HttpPut("{groupId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateGroup([FromRoute] Guid groupId, [FromBody] UpdateGroupRequestDTO request)
    {
        _logger.LogInformation("Admin is attempting to update group with ID: {GroupId}", groupId);
        var result = await _adminGroupService.UpdateGroupInfoAsync(groupId, request);
        if (result.Success) return Ok(result);
        return result.Errors!.First().ErrorCode == "GROUP_NOT_FOUND" ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// [Admin] Lưu trữ một nhóm.
    /// </summary>
    /// <param name="groupId">ID của nhóm được lấy từ URL</param>
    [HttpPatch("{groupId}/archive")] 
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)] 
    public async Task<IActionResult> ArchiveGroup(Guid groupId)
    {
        _logger.LogInformation("Admin is attempting to archive group with ID: {GroupId}", groupId);
        var (adminId, adminFullName) = GetAdminInfo();
        var result = await _adminGroupService.ArchiveGroupAsAdminAsync(groupId, adminId, adminFullName);
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.ErrorCode == "GROUP_NOT_FOUND") ?? false)
                return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Bỏ lưu trữ một nhóm.
    /// </summary>
    /// <param name="groupId">ID của nhóm được lấy từ URL</param>
    [HttpPatch("{groupId}/unarchive")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> UnarchiveGroup(Guid groupId)
    {
        _logger.LogInformation("Admin is attempting to unarchive group with ID: {GroupId}", groupId);
        var (adminId, adminFullName) = GetAdminInfo();
        var result = await _adminGroupService.UnarchiveGroupAsAdminAsync(groupId, adminId, adminFullName);
        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.ErrorCode == "GROUP_NOT_FOUND") ?? false)
                return NotFound(result);

            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Lấy thông tin chi tiết của một nhóm.
    /// </summary>
    /// <response code="200">Trả về thông tin chi tiết của nhóm.</response>
    /// <response code="404">Không tìm thấy nhóm với ID cung cấp.</response>
    [ProducesResponseType(typeof(ApiResponse<AdminGroupDetailDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [HttpGet("{groupId:guid}")]
    public async Task<IActionResult> GetGroupDetails([FromRoute]Guid groupId)
    {
        _logger.LogInformation("Admin is attempting to get details for group with ID: {GroupId}", groupId);
        var result = await _adminGroupService.GetGroupDetailsAsync(groupId);
        return result.Success ? Ok(result) : NotFound(result);
    }


    /// <summary>
    /// [Admin] Lấy danh sách thành viên trong nhóm.
    /// </summary>
    /// <response code="200">Trả về danh sách thành viên.</response>
    /// <response code="404">Không tìm thấy nhóm.</response>
    [HttpGet("{groupId:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<GroupAdminMemberDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupMembers([FromRoute] Guid groupId, [FromQuery] GetGroupMembersParams request)
    {
        _logger.LogInformation("Admin is attempting to get members for group with ID: {GroupId}", groupId);
        var result = await _adminGroupService.GetGroupMembersAsync(groupId, request);
        return Ok(result);
    }

    /// <summary>
    /// [Admin tổng] Thêm một người dùng vào một nhóm bất kỳ.
    /// </summary>
    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMemberToGroup(Guid groupId, [FromBody] AddMemberAdminRequest request)
    {
        _logger.LogInformation("Admin is attempting to add member to group with ID: {GroupId}", groupId);
        var result = await _adminGroupService.AddMemberAsSystemAdminAsync(groupId, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
    /// <summary>
    /// [Admin] Cập nhật vai trò của một thành viên.
    /// </summary>
    /// <response code="200">Cập nhật vai trò thành công.</response>
    /// <response code="404">Không tìm thấy nhóm hoặc thành viên.</response>
    /// <response code="400">Vai trò mới không hợp lệ.</response>
    [HttpPut("{groupId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMemberRole([FromRoute] Guid groupId,[FromRoute] Guid userId, [FromBody] UpdateMemberRoleDTO dto)
    {
        _logger.LogInformation("Admin is attempting to update role for user {UserId} in group {GroupId}", userId, groupId);
        var result = await _adminGroupService.UpdateMemberRoleAsync(groupId, userId, dto.NewRole);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// [Admin] Xóa một thành viên khỏi nhóm.
    /// </summary>
    /// <response code="204">Xóa thành viên thành công.</response>
    /// <response code="404">Không tìm thấy nhóm hoặc thành viên.</response>
    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid groupId, [FromRoute] Guid userId)
    {
        _logger.LogInformation("Admin is attempting to remove user {UserId} from group {GroupId}", userId, groupId);
        var result = await _adminGroupService.RemoveMemberAsync(groupId, userId);
        return result.Success ? NoContent() : NotFound(result);
    }
    // --- Các API quản lý bài viết trong nhóm ---

    /// <summary>
    /// [Admin] Lấy danh sách bài viết trong nhóm.
    /// </summary>
    /// <response code="200">Trả về danh sách bài viết.</response>
    /// <response code="404">Không tìm thấy nhóm.</response>
    [HttpGet("{groupId:guid}/posts")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PostForListDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupPosts([FromRoute] Guid groupId, [FromQuery] GetGroupPostsParams request)
    {
        _logger.LogInformation("Admin is attempting to get posts for group with ID: {GroupId}", groupId);
        var result = await _adminGroupService.GetGroupPostsAsync(groupId, request);
        return Ok(result);
    }
    /// <summary>
    /// [Admin] Lấy thông tin chi tiết của một bài viết để kiểm duyệt.
    /// </summary>
    /// <param name="postId">ID của bài viết cần xem.</param>
    [HttpGet("{postId:int}/posts")]
    public async Task<IActionResult> GetPostDetails(int postId)
    {
        var result = await _adminGroupService.GetPostDetailsAsAdminAsync(postId);
        return result.Success ? Ok(result) : NotFound(result);
    }
    /// <summary>
    /// [Admin] Xóa một bài viết trong nhóm.
    /// </summary>
    /// <response code="204">Xóa bài viết thành công.</response>
    /// <response code="404">Không tìm thấy bài viết.</response>
    [HttpDelete("{groupId:guid}/posts/{postId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost([FromRoute] Guid groupId, int postId)
    {
        _logger.LogInformation("Admin is attempting to delete post with ID: {PostId} in group {GroupId}", postId, groupId);
        var result = await _adminGroupService.DeletePostAsync(postId);
        return result.Success ? NoContent() : NotFound(result);
    }

    [HttpPatch("{groupId:guid}/posts/{postId:int}/restore")]
    public async Task<IActionResult> RestorePost([FromRoute] Guid groupId, int postId)
    {
        _logger.LogInformation("Admin is attempting to restore post with ID: {PostId} in group {GroupId}", postId, groupId);
        var result = await _adminGroupService.RestorePostAsync(postId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    // --- Các API quản lý cài đặt nhóm ---

    /// <summary>
    /// [Admin] Cập nhật loại hình của nhóm (Public/Private).
    /// </summary>
    /// <response code="200">Cập nhật thành công.</response>
    [HttpPut("{groupId:guid}/settings")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGroupSettings(Guid groupId, [FromBody] UpdateGroupSettingsDTO dto)
    {
        _logger.LogInformation("Admin is attempting to update settings for group with ID: {GroupId}", groupId);
        var result = await _adminGroupService.UpdateGroupSettingsAsync(groupId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// [Admin] Tìm kiếm thành viên bên trong một nhóm cụ thể.
    /// </summary>
    /// <param name="groupId">ID của nhóm cần tìm kiếm.</param>
    /// <param name="request">Chứa từ khóa tìm kiếm và thông tin phân trang.</param>
    [HttpGet("{groupId:guid}/members/search")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SearchGroupMembers([FromRoute] Guid groupId, [FromQuery] SearchGroupMembersParams request)
    {
        var result = await _adminGroupService.SearchMembersInGroupAsync(groupId, request);
        return Ok(result);
    }
    /// <summary>
    /// [Admin] Thay đổi chủ sở hữu của nhóm.
    /// </summary>
    /// <remarks>Hành động này cần được thực hiện cẩn thận. Chủ sở hữu cũ sẽ trở thành Admin.</remarks>
    /// <response code="200">Thay đổi chủ sở hữu thành công.</response>
    /// <response code="404">Không tìm thấy nhóm hoặc chủ sở hữu mới.</response>
    [HttpPut("{groupId:guid}/owner")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeGroupOwner([FromRoute] Guid groupId, [FromBody] ChangeGroupOwnerDTO dto)
    {
        _logger.LogInformation("Admin is attempting to change owner for group with ID: {GroupId} to user {NewOwnerUserId}", groupId, dto.NewOwnerUserId);
        var result = await _adminGroupService.ChangeGroupOwnerAsync(groupId, dto.NewOwnerUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// [Admin] Thực hiện một hành động hàng loạt trên nhiều nhóm.
    /// </summary>
    [HttpPost("bulk-action")]
    public IActionResult PerformBulkGroupAction([FromBody] BulkGroupActionRequest request)
    {
        _logger.LogInformation("Admin is attempting to perform bulk action: {Action} on groups: {@GroupIds}", request.Action, request.GroupIds);
        var result = _adminGroupService.PerformBulkGroupActionAsync(request);
        return Ok(result);
    }

    private (Guid, string) GetAdminInfo()
    {
        if (!Guid.TryParse(_currentUser.Id, out Guid adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            throw new UnauthorizedAccessException("Unable to retrieve current admin user information.");
        }
        var adminFullName = _currentUser.FullName;
        return (adminId, adminFullName);
    }
}
