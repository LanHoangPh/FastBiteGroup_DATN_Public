using FastBiteGroupMCA.Application.DTOs.Admin.Group;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.Group.Admin;
using FastBiteGroupMCA.Application.DTOs.Invitation;
using FastBiteGroupMCA.Application.DTOs.Post;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Infastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/groups")]
[Produces("application/json")]
[Authorize]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IDashboardService _dashboardService;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<GroupsController> _logger;
    private readonly IPostService _postService;
    public GroupsController(IGroupService groupService, ILogger<GroupsController> logger, IPostService postService, IInvitationService invitationService, IDashboardService dashboardService)
    {
        _groupService = groupService;
        _logger = logger;
        _invitationService = invitationService;
        _postService = postService;
        _dashboardService = dashboardService;
    }
    // === Quản lý Nhóm Chat ===

    /// <summary>
    /// Lấy danh sách các nhóm công khai bao gogòm cả nhóm chat và cộng đồng (có phân trang và tìm kiếm).
    /// </summary>
    [HttpGet("public")]
    [Tags("Groups.Discovery")]
    [AllowAnonymous] 
    public async Task<IActionResult> GetPublicGroups([FromQuery] GetPublicGroupsQuery query)
    {
        var response = await _groupService.GetPublicGroupsAsync(query);
        return Ok(response);
    }

    /// <summary>
    /// Tạo một nhóm trò chuyện mới (Public hoặc Private).
    /// </summary>
    [HttpPost("chat")]
    [Tags("Groups.Management")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateChatGroup([FromForm] CreateChatGroupDto dto)
    {
        var response = await _groupService.CreateChatGroupAsync(dto);
        if (!response.Success) return BadRequest(response);
        // Trả về response 201 Created với link để lấy thông tin nhóm mới
        return CreatedAtAction(nameof(GetGroupById), new { groupId = response.Data!.GroupId }, response);
    }


    /// <summary>
    /// Tạo một nhóm cộng đồng mới (chuyên về bài đăng).
    /// </summary>
    [HttpPost("community")]
    [Tags("Groups.Management")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCommunityGroup([FromForm] CreateCommunityGroupDto dto)
    {
        var response = await _groupService.CreateCommunityGroupAsync(dto);
        if (!response.Success) return BadRequest(response);
        return CreatedAtAction(nameof(GetGroupById), new { groupId = response.Data!.GroupId }, response);
    }
    /// <summary>
    /// Thêm một thành viên mới vào một nhóm.
    /// </summary>
    /// <remarks>
    /// Chỉ Admin hoặc Moderator của nhóm mới có quyền thực hiện hành động này.
    /// </remarks>
    /// <param name="groupId">ID của nhóm cần thêm thành viên vào.</param>
    /// <param name="request">Chứa ID của người dùng sẽ được thêm.</param>
    [HttpPost("{groupId:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember([FromRoute] Guid groupId, [FromBody] AddMemberRequestDto request)
    {
        var result = await _groupService.AddMemberAsync(groupId, request.UserIdToAdd);

        if (!result.Success)
        {
            if (result.Errors?.Any(e => e.ErrorCode == "FORBIDDEN") ?? false)
                return Forbid();

            if (result.Errors?.Any(e => e.ErrorCode.Contains("NOT_FOUND")) ?? false)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
    /// <summary>
    /// Lấy thông tin chi tiết của một nhóm.
    /// </summary>
    [HttpGet("{groupId:guid}")]
    [Tags("Groups.Management")]
    public async Task<IActionResult> GetGroupById(Guid groupId)
    {
        var response = await _groupService.GetGroupDetailsByIdAsync(groupId);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một nhóm.
    /// </summary>
    [HttpGet("{groupId:guid}/preview")]
    [Tags("Groups.Management")]
    public async Task<IActionResult> GetGroupByIdPreView(Guid groupId)
    {
        var response = await _groupService.GetGroupDetailsByIdAsyncPreView(groupId);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }
    // === Quản lý Thành viên & Hành động ===

    /// <summary>
    /// Lấy danh sách thành viên của một nhóm.
    /// </summary>
    [HttpGet("{groupId:guid}/members")]
    [Tags("Groups.Membership")]
    public async Task<IActionResult> GetGroupMembers(Guid groupId, [FromQuery] GetGroupMembersQuery query)
    {
        var response = await _groupService.GetGroupMembersAsync(groupId, query);
        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "ACCESS_DENIED") return Forbid();
            return BadRequest(response);
        }
        return Ok(response);
    }

    /// <summary>
    /// Tham gia vào một nhóm công khai.
    /// </summary>
    [HttpPost("{groupId:guid}/join")]
    [Tags("Groups.Membership")]
    public async Task<IActionResult> JoinGroup(Guid groupId)
    {
        try
        {
            var response = await _groupService.JoinPublicGroupAsync(groupId);
            if (!response.Success)
            {
                return response.Errors?.FirstOrDefault()?.ErrorCode switch
                {
                    "GroupNotFound" => NotFound(response),
                    "NotPublicGroup" => Forbid(),
                    "AlreadyMember" => Conflict(response),
                    _ => BadRequest(response)
                };
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ");
            return StatusCode(500, ApiResponse<object>.Fail("FATAL_ERROR", "Một lỗi nghiêm trọng đã xảy ra trong controller."));
        }
    }

    /// <summary>
    /// Rời khỏi một nhóm.
    /// </summary>
    [HttpPost("{groupId:guid}/leave")]
    [Tags("Groups.Membership")]
    public async Task<IActionResult> LeaveGroup(Guid groupId)
    {
        var response = await _groupService.LeaveGroupAsync(groupId);

        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "LAST_ADMIN_LEAVE_ATTEMPT")
            {
                return BadRequest(response);
            }
            return StatusCode(500, response);
        }

        return Ok(response);
    }

    /// <summary>
    /// (Admin cuối cùng) Chuyển quyền và rời nhóm.
    /// </summary>
    [HttpPost("{groupId:guid}/transfer-and-leave")]
    [Tags("Groups.Membership")]
    public async Task<IActionResult> TransferAndLeave(Guid groupId, [FromBody] TransferAndLeaveDTO dto)
    {
        var response = await _groupService.TransferAndLeaveAsync(groupId, dto);

        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "Forbidden" || errorCode == "NotLastAdmin")
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// (Host/Admin) Quản lý vai trò của thành viên trong nhóm.
    /// </summary>
    [HttpPut("{groupId:guid}/members/{memberId:guid}/role")]
    [Tags("Groups.Membership")]
    public async Task<IActionResult> ManageMember(Guid groupId, Guid memberId, [FromBody] ManageMemberDTO dto)
    {
        var response = await _groupService.ManageMemberRoleAsync(groupId, memberId, dto);

        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "Forbidden" || errorCode == "CannotChangeAdminRole")
            {
                return Forbid();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Xóa vĩnh viễn một nhóm và toàn bộ dữ liệu liên quan.
    /// </summary>
    /// <remarks>
    /// HÀNH ĐỘNG NGUY HIỂM, KHÔNG THỂ PHỤC HỒI.
    /// Chỉ Admin của nhóm mới có quyền thực hiện.
    /// </remarks>
    [HttpDelete("{groupId:guid}")]
    [Tags("Groups.Management")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    {
        var response = await _groupService.DeleteGroupAsync(groupId);

        if (!response.Success)
        {
            // Xử lý lỗi chi tiết (NotFound, Forbidden...)
            return BadRequest(response);
        }

        return NoContent();
    }

    /// <summary>
    /// (Admin) Lưu trữ một nhóm.
    /// </summary>
    [HttpPost("{groupId:guid}/archive")]
    [Tags("Groups.Management")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ArchiveGroup(Guid groupId)
    {
        var response = await _groupService.ArchiveGroupAsync(groupId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return NoContent(); 
    }

    /// <summary>
    /// (Admin) Khôi phục một nhóm đã bị lưu trữ.
    /// </summary>
    [HttpPost("{groupId:guid}/unarchive")]
    [Tags("Groups.Management")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnarchiveGroup(Guid groupId)
    {
        var response = await _groupService.UnarchiveGroupAsync(groupId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return NoContent();
    }

    /// <summary>
    /// Xóa một thành viên ra khỏi nhóm (chỉ dành cho Admin/Moderator).
    /// </summary>
    /// <param name="groupId">ID của nhóm.</param>
    /// <param name="userIdToKick">ID của người dùng cần xóa.</param>
    [HttpDelete("{groupId:guid}/members/{userIdToKick:guid}")]
    [Tags("Groups.Membership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> KickMember(Guid groupId, Guid userIdToKick)
    {
        var response = await _groupService.KickMemberAsync(groupId, userIdToKick);

        if (response.Success)
        {
            return NoContent();
        }
        return response.Errors?.FirstOrDefault()?.ErrorCode switch
        {
            "TARGET_NOT_FOUND" or "GROUP_NOT_FOUND" => NotFound(response),
            "FORBIDDEN" or "NOT_A_MEMBER" => Forbid(),
            _ => BadRequest(response)
        };
    }

    /// <summary>
    /// Lấy gợi ý thành viên để @mention.
    /// </summary>
    [HttpGet("{groupId:guid}/mention-suggestions")]
    [Tags("Groups.Membership")]
    public async Task<IActionResult> GetMentionSuggestions(Guid groupId, [FromQuery] string? search)
    {
        var result = await _groupService.GetMentionSuggestionsAsync(groupId, search);
        return Ok(result);
    }

    /// <summary>
    /// [Admin/Mod] Lấy danh sách các lời mời đã gửi đi của một nhóm.
    /// </summary>
    /// <remarks>
    /// Trả về danh sách các lời mời đã được gửi đi cho một nhóm cụ thể.
    /// Yêu cầu người dùng phải là Admin hoặc Moderator của nhóm.
    /// Hỗ trợ phân trang, tìm kiếm theo tên người nhận và lọc theo trạng thái.
    /// </remarks>
    /// <param name="groupId">ID của nhóm.</param>
    /// <param name="query">Tham số truy vấn (phân trang, lọc, tìm kiếm).</param>
    [HttpGet("{groupId}/invitations/sent")]
    [Tags("Groups.Management")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SentGroupInvitationDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSentInvitationsForGroup(
        [FromRoute] Guid groupId,
        [FromQuery] GetSentInvitationsQuery query)
    {
        var response = await _invitationService.GetSentInvitationsByGroupAsync(groupId, query);
        // Controller sẽ trả về kết quả từ service (có thể là success hoặc fail)
        return Ok(response);
    }

    /// <summary>
    /// Thu hồi một lời mời đã gửi đang ở trạng thái chờ.
    /// </summary>
    /// <param name="groupId">ID của nhóm.</param>
    /// <param name="invitationId">ID của lời mời cần thu hồi.</param>
    [HttpDelete("{groupId}/invitations/{invitationId}")]
    [Tags("Groups.Management")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInvitation(
        [FromRoute] Guid groupId,
        [FromRoute] int invitationId)
    {
        var response = await _invitationService.RevokeInvitationAsync(groupId, invitationId);
        return Ok(response);
    }

    /// <summary>
    /// [Admin/Mod] Lấy dữ liệu thống kê cho dashboard quản lý nhóm.
    /// </summary>
    [HttpGet("{groupId:guid}/management/dashboard")]
    [Tags("Groups.Management")]
    [ProducesResponseType(typeof(ApiResponse<GroupDashboardDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupDashboardStats(Guid groupId)
    {
        var response = await _dashboardService.GetGroupDashboardStatsAsync(groupId);
        return Ok(response);
    }
    /// <summary>
    /// [Admin/Mod] Lấy tổng quan về các hoạt động quản trị và các vấn đề cần chú ý trong nhóm.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    [HttpGet("{groupId:guid}/management/moderation-overview")]
    [Tags("Groups.Management")]
    [ProducesResponseType(typeof(ApiResponse<ModerationOverviewDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModerationOverview(Guid groupId)
    {
        var response = await _dashboardService.GetModerationOverviewAsync(groupId);
        return Ok(response);
    }

    ///// <summary>
    ///// [Admin/Mod] Cập nhật thông tin cài đặt của nhóm.
    ///// </summary>
    //[HttpPut("{groupId:guid}/management/settings")]
    //[Tags("Groups.Management")]
    //public async Task<IActionResult> UpdateGroupSettings(Guid groupId, [FromBody] UpdateGroupSettingsDTO dto)
    //{
    //    var response = await _groupManagementService.UpdateSettingsAsync(groupId, dto); // Giả sử có một service riêng
    //    return Ok(response);
    //}

    // === Quản lý Lời mời ===

    // api tìm kiếm ngưới dùng có dùng ở controller  UserController api SearchUsersForInvite

    /// <summary>
    /// Gửi lời mời trực tiếp đến người dùng.
    /// Với nhóm Công khai (Public) ai cũng thể gửi lời mời. Với nhóm Riêng tư (Private) chỉ Admin và Moderator mới có quyền này.
    /// </summary>
    [HttpPost("{groupId:guid}/invitations")]
    [Tags("Groups.Invitations")]
    public async Task<IActionResult> SendInvitations(Guid groupId, [FromBody] SendInvitationsDto dto)
    {
        var response = await _groupService.SendInvitationsAsync(groupId, dto);

        if (!response.Success && response.Errors?.FirstOrDefault()?.ErrorCode == "Forbidden")
        {
            return Forbid();
        }

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
    /// <summary>
    /// Tạo một liên kết mời tham gia nhóm.
    /// </summary>
    [HttpPost("{groupId:guid}/invite-links")]
    [Tags("Groups.Invitations")]
    public async Task<IActionResult> CreateInviteLink(Guid groupId, [FromBody] CreateInviteLinkDTO dto)
    {
        var response = await _groupService.CreateInviteLinkAsync(groupId, dto);

        if (!response.Success && response.Errors?.FirstOrDefault()?.ErrorCode == "Forbidden")
            return Forbid();

        if (!response.Success)
            return BadRequest(response);

        // Trả về 201 Created vì đây là hành động tạo tài nguyên mới
        return CreatedAtAction(nameof(CreateInviteLink), new { groupId = groupId }, response);
    }

    // === Tiện ích ===
    /// <summary>
    /// Cập nhật ảnh đại diện của nhóm.
    /// </summary>
    [HttpPut("{groupId:guid}/avatar")] // Dùng PUT để cập nhật tài nguyên
    [Tags("Groups.Utilities")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateGroupAvatar(Guid groupId, IFormFile file)
    {
        var result = await _groupService.UpdateGroupAvatarAsync(groupId, file);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Cập nhật thông tin của một nhóm (tên, mô tả, quyền riêng tư).
    /// </summary>
    [HttpPut("{groupId:guid}")]
    [Tags("Groups.Management")]
    public async Task<IActionResult> UpdateGroupInfo(Guid groupId, [FromBody] UpdateGroupInfoDto dto)
    {
        var response = await _groupService.UpdateGroupInfoAsync(groupId, dto);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return NoContent(); // Trả về 204 No Content khi cập nhật thành công
    }

    

    // === Quản lý Bài viết của Nhóm ===
    /// <summary>
    /// Lấy danh sách bài viết trong một nhóm (có phân trang).
    /// </summary>
    [HttpGet("{groupId:guid}/posts")]
    [Tags("Groups.Posts")]
    public async Task<IActionResult> GetGroupPosts(Guid groupId, [FromQuery] GetPostsInGroupQuery query)
    {
        var response = await _postService.GetPostsForGroupAsync(groupId, query);

        if (!response.Success && response.Errors?.FirstOrDefault()?.ErrorCode == "Forbidden")
        {
            return Forbid(); 
        }
        return Ok(response);
    }
    /// <summary>
    /// Tạo một bài viết mới trong nhóm.
    /// </summary>
    [HttpPost("{groupId:guid}/posts")]
    [Tags("Groups.Posts")]
    public async Task<IActionResult> CreatePost(Guid groupId, [FromBody] CreatePostDTO dto)
    {
        var response = await _postService.CreatePostAsync(groupId, dto);

        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            return errorCode == "Forbidden" ? Forbid() : BadRequest(response);
        }

        return CreatedAtAction("GetPostById", "Posts", new { postId = response.Data!.PostId }, response);
    }
}
