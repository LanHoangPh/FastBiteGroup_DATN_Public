using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.Invitation;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Infastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/invitations")]
[Produces("application/json")]
[Authorize]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _inviteService;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(IInvitationService inviteService, ILogger<InvitationsController> logger)
    {
        _inviteService = inviteService;
        _logger = logger;
    }
    // === Lời mời Trực tiếp ===
    
    /// <summary>
    /// Lấy danh sách các lời mời vào nhóm đang chờ xử lý của tôi.
    /// </summary>
    /// <response code="200">Trả về danh sách các lời mời đang chờ.</response>
    /// <response code="401">Chưa xác thực.</response>
    [HttpGet("me")]
    [Tags("Invitations.Direct")]
    [ProducesResponseType(typeof(ApiResponse<List<GroupInvitationDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPendingInvitationsAsync()
    {
        var response = await _inviteService.GetPendingInvitationsAsync();
        return Ok(response);
    }

    [HttpPost("{invitationId:int}/respond")]
    [Tags("Invitations.Direct")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondToGroupInvitation(int invitationId, [FromBody] RespondToInvitationDTO dto)
    {
        var response = await _inviteService.RespondToInvitationAsync(invitationId, dto);

        if (response.Success)
        {
            return NoContent(); // Trả về 204 No Content khi thành công
        }

        // Xử lý lỗi dựa trên ErrorCode
        return response.Errors?.FirstOrDefault()?.ErrorCode switch
        {
            "INVALID_INVITATION" => NotFound(response),
            _ => BadRequest(response)
        };
    }

    // === Link mời ===

    /// <summary>
    /// Lấy thông tin xem trước của nhóm từ một mã mời.
    /// </summary>
    /// <remarks>API này không yêu cầu xác thực, cho phép người dùng xem thông tin nhóm trước khi quyết định tham gia.</remarks>
    /// <param name="invitationCode">Mã mời từ đường link.</param>
    /// <response code="200">Trả về thông tin xem trước của nhóm.</response>
    /// <response code="404">Mã mời không hợp lệ hoặc đã hết hạn.</response>
    [HttpGet("link/{invitationCode}")]
    [Tags("Invitations.Link")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<GroupPreviewDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvitationLinkInfoAsync(string invitationCode)
    {
        var response = await _inviteService.GetGroupPreviewByCodeAsync(invitationCode);

        if (!response.Success)
        {
            // Nếu không thành công (mã mời sai, nhóm không tồn tại), trả về 404
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Chấp nhận lời mời và tham gia nhóm thông qua mã mời.
    /// </summary>
    /// <remarks>Người dùng phải đăng nhập để thực hiện hành động này.</remarks>
    /// <param name="invitationCode">Mã mời từ đường link.</param>
    /// <response code="200">Tham gia nhóm thành công.</response>
    /// <response code="404">Mã mời không hợp lệ hoặc đã hết hạn.</response>
    /// <response code="409">Người dùng đã là thành viên của nhóm này.</response>
    [HttpPost("link/{invitationCode}/accept")]
    [Tags("Invitations.Link")]
    public async Task<IActionResult> AcceptInvite(string invitationCode)
    {
        _logger.LogInformation("User {UserId} is attempting to accept invite with code {InvitationCode}", User.Identity?.Name, invitationCode);
        var response = await _inviteService.AcceptInviteLinkAsync(invitationCode);
        if (!response.Success)
        {
            return response.Errors?.FirstOrDefault()?.ErrorCode switch
            {
                "InvalidLink" => NotFound(response),
                "ExpiredLink" => NotFound(response),
                "UsageLimitExceeded" => NotFound(response),
                "AlreadyMember" => Conflict(response),
                "Conflict" => Conflict(response),
                _ => BadRequest(response)
            };
        }
        return Ok(response);

    }
}
