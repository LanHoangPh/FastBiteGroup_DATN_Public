using FastBiteGroupMCA.Application.DTOs.VideoCall;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/video-calls")]
[Produces("application/json")]
[Authorize]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class VideoCallsController : ControllerBase
{
    private readonly IVideoCallService _videoCallService;

    public VideoCallsController(IVideoCallService videoCallService)
    {
        _videoCallService = videoCallService;
    }
    /// <summary>
    /// Tham gia một phiên gọi video đang diễn ra.
    /// </summary>
    /// <param name="sessionId">ID của phiên gọi video.</param>
    /// <response code="200">Tham gia thành công. Trả về LiveKit token.</response>
    /// <response code="403">Không có quyền tham gia.</response>
    /// <response code="404">Phiên gọi không tồn tại hoặc đã kết thúc.</response>
    [HttpPost("{sessionId:guid}/join")]
    [Tags("Video Calls")]
    [ProducesResponseType(typeof(ApiResponse<JoinCallResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> JoinCall(Guid sessionId)
    {
        var result = await _videoCallService.JoinCallGroupAsync(sessionId);

        // Dùng switch expression để trả về mã lỗi HTTP phù hợp hơn
        return result.Success ? Ok(result) : result.Errors?.FirstOrDefault()?.ErrorCode switch
        {
            "CALL_NOT_FOUND" => NotFound(result),
            "CALL_ENDED" => NotFound(result),
            "FORBIDDEN" => Forbid(), // Trả về 403 Forbidden
            _ => BadRequest(result)
        };
    }

    /// <summary>
    /// (Host/Admin) Tắt mic của một người tham gia(Group).
    /// </summary>
    [HttpPost("{sessionId:guid}/participants/{targetUserId:guid}/mute-mic")]
    [Tags("Video Calls")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MuteParticipantMic(Guid sessionId, Guid targetUserId)
    {
        var result = await _videoCallService.MuteParticipantTrackAsync(sessionId, targetUserId, TrackSource.Microphone);
        return result.Success ? NoContent() : Forbid();
    }
    /// <summary>
    /// (Host/Admin) Tắt camera của một người tham gia(Group).
    /// </summary>
    [HttpPost("{sessionId:guid}/participants/{targetUserId:guid}/stop-video")]
    [Tags("Video Calls")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StopParticipantVideo(Guid sessionId, Guid targetUserId)
    {
        var result = await _videoCallService.MuteParticipantTrackAsync(sessionId, targetUserId, TrackSource.Camera);
        return result.Success ? NoContent() : Forbid();
    }
    /// <summary>
    /// (Host/Admin) Xóa một người tham gia khỏi cuộc gọi(Group).
    /// </summary>
    [HttpDelete("{sessionId:guid}/participants/{targetUserId:guid}")]
    [Tags("Video Calls")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveParticipant(Guid sessionId, Guid targetUserId)
    {
        var result = await _videoCallService.RemoveParticipantAsync(sessionId, targetUserId);
        return result.Success ? NoContent() : Forbid();
    }
    /// <summary>
    /// (Host/Admin) Kết thúc phiên gọi cho tất cả mọi người(Group).
    /// </summary>
    [HttpPost("{sessionId:guid}/end")]
    [Tags("Video Calls")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EndCallForAll(Guid sessionId)
    {
        var result = await _videoCallService.EndCallForAllAsync(sessionId);
        return result.Success ? Ok(result) : Forbid();
    }

    /// <summary>
    /// Chấp nhận một lời mời gọi video trực tiếp(1-1).
    /// </summary>
    [HttpPost("{sessionId:guid}/accept")]
    [Tags("Video Calls")]
    [ProducesResponseType(typeof(ApiResponse<AcceptCallResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptDirectCall(Guid sessionId)
    {
        var result = await _videoCallService.AcceptDirectCallAsync(sessionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// (Người nhận) Từ chối một cuộc gọi đang đổ chuông(1-1).
    /// </summary>
    [HttpPost("{sessionId:guid}/decline")]
    [Tags("Video Calls")]
    public async Task<IActionResult> DeclineDirectCall(Guid sessionId)
    {
        var result = await _videoCallService.DeclineDirectCallAsync(sessionId);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return NoContent();
    }

    /// <summary>
    /// Rời khỏi hoặc hủy một cuộc gọi(Áp dụng đc cả 1-1 và Group).
    /// </summary>
    [HttpPost("{sessionId:guid}/leave")]
    [Tags("Video Calls")]
    public async Task<IActionResult> LeaveCall(Guid sessionId)
    {
        var result = await _videoCallService.LeaveCallAsync(sessionId);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return NoContent();
    }
}
