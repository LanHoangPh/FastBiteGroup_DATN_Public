using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Conversation;
using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.DTOs.Poll;
using FastBiteGroupMCA.Application.DTOs.VideoCall;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/conversations")] 
[Produces("application/json")]
[Authorize]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IMessageService _messageService;
    private readonly IVideoCallService _videoCallService;
    private readonly IPollService _pollService;

    public ConversationsController(IConversationService conversationService, IMessageService messageService, IVideoCallService videoCallService, IPollService pollService)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _videoCallService = videoCallService;
        _pollService = pollService;
    }

    /// <summary>
    /// Tìm hoặc tạo cuộc hội thoại trực tiếp (1-1) với một người dùng khác.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("direct")]
    [Tags("Conversations.Management")]
    [ProducesResponseType(typeof(ApiResponse<ConversationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConversationResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FindOrCreateDirectConversation([FromBody] CreateDirectConversationDto dto)
    {
        var result = await _conversationService.FindOrCreateDirectConversationAsync(dto.PartnerUserId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        if (result.Data!.WasCreated)
        {
            return StatusCode(201, result);
        }

        return Ok(result);
    }
    /// <summary>
    /// Xóa (ẩn) một cuộc hội thoại 1-1 khỏi danh sách của người dùng hiện tại.
    /// </summary>
    /// <remarks>
    /// Hành động này không xóa vĩnh viễn dữ liệu, chỉ ẩn đi. 
    /// Nếu người kia nhắn tin lại, cuộc hội thoại sẽ xuất hiện trở lại.
    /// </remarks>
    /// <param name="conversationId">ID của cuộc hội thoại cần xóa.</param>
    [HttpDelete("{conversationId:int}")]
    [Tags("Conversations.Management")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteConversation(int conversationId)
    {
        var response = await _conversationService.DeleteConversationForCurrentUserAsync(conversationId);

        if (!response.Success)
        {
            // ... xử lý lỗi NotFound, Forbidden ...
            return BadRequest(response);
        }

        return NoContent(); // Trả về 204 No Content khi thành công
    }
    /// <summary>
    /// Lấy danh sách các cuộc hội thoại của người dùng hiện tại.
    /// </summary>
    /// <remarks>
    /// ### Các tùy chọn lọc (filter):
    /// - **direct**: Chỉ lấy các cuộc hội thoại 1-1.
    /// - **group**: Chỉ lấy các cuộc hội thoại nhóm.
    /// - Nếu để trống, API sẽ trả về tất cả các cuộc hội thoại.
    /// </remarks>
    /// <param name="query">Chuỗi dùng để lọc loại hội thoại.</param>
    /// <response code="200">Trả về danh sách các cuộc hội thoại thành công.</response>
    /// <response code="401">Chưa xác thực.</response>
    [HttpGet("me")]
    [Tags("Conversations.Management")]
    [ProducesResponseType(typeof(ApiResponse<List<ConversationListItemDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyConversations([FromQuery] GetMyConversationsQuery query)
    {
        var result = await _conversationService.GetMyConversationsAsync(query);
        return Ok(result);
    }
    /// <summary>
    /// Lấy thông tin chi tiết của một cuộc hội thoại.
    /// </summary>
    /// <remarks>
    /// Trả về thông tin của cuộc hội thoại và trang đầu tiên của lịch sử tin nhắn.
    /// </remarks>
    /// <param name="conversationId">ID của cuộc hội thoại.</param>
    /// <param name="query">Tham số phân trang cho tin nhắn (ví dụ: pageSize).</param>
    [HttpGet("{conversationId:int}")]
    [Tags("Conversations.Management")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationDetails(int conversationId, [FromQuery] GetConversationMessagesQuery query)
    {
        var result = await _conversationService.GetConversationDetailsAsync(conversationId, query);
        // ... xử lý lỗi NotFound, Forbidden ...
        return Ok(result);
    }

    /// <summary>
    /// Lấy lịch sử tin nhắn của một cuộc hội thoại (hỗ trợ "infinite scroll").
    /// </summary>
    /// <remarks>
    /// API này sử dụng phương pháp Cursor Pagination để tải tin nhắn.
    /// - **Lần tải đầu tiên**: Để trống tham số `beforeMessageId`.
    /// - **Để tải các tin nhắn cũ hơn**: Sử dụng giá trị `nextCursor` từ lần phản hồi trước làm `beforeMessageId` cho lần gọi tiếp theo.
    /// </remarks>
    /// <param name="conversationId">ID của cuộc hội thoại.</param>
    /// <param name="query">Tham số truy vấn cho việc phân trang.</param>
    /// <response code="200">Trả về một phần lịch sử tin nhắn thành công.</response>
    /// <response code="401">Chưa xác thực.</response>
    /// <response code="403">Không có quyền xem cuộc hội thoại này.</response>
    [HttpGet("{conversationId:int}/messages")]
    [Tags("Conversations.Messaging & Files")]
    [Authorize(Policy = "IsConversationMember")] // Đảm bảo chỉ thành viên mới được xem tin nhắn
    [ProducesResponseType(typeof(ApiResponse<MessageHistoryResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessagesByConversation(int conversationId, [FromQuery] GetMessagesQuery query)
    {
        var result = await _messageService.GetMessageHistoryAsync(conversationId, query);
        return Ok(result);
    }

    /// <summary>
    /// Gửi một tin nhắn vào cuộc hội thoại.
    /// </summary>
    /// <remarks>
    /// API này sẽ gửi tin nhắn và phát một sự kiện real-time (qua SignalR) đến các thành viên khác trong cuộc hội thoại.
    /// Tin nhắn được tạo thành công sẽ có mã trạng thái là 201 Created.
    /// </remarks>
    /// <param name="conversationId">ID của cuộc hội thoại.</param>
    /// <param name="request">Nội dung tin nhắn cần gửi.</param>
    /// <response code="201">Gửi tin nhắn thành công.</response>
    /// <response code="400">Nội dung không hợp lệ hoặc có lỗi xảy ra.</response>
    /// <response code="401">Chưa xác thực.</response>
    /// <response code="403">Không phải là thành viên của cuộc hội thoại này.</response>
    [HttpPost("{conversationId:int}/messages")]
    [Tags("Conversations.Messaging & Files")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(typeof(ApiResponse<MessageDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendMessage([FromRoute] int conversationId, [FromBody] SendMessageDTO request)
    {
        var result = await _messageService.SendMessageAsync(conversationId, request);

        if (!result.Success)
        {
            return result.Errors?.FirstOrDefault()?.ErrorCode switch
            {
                "FORBIDDEN" => Forbid(),
                _ => BadRequest(result)
            };
        }
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Tìm kiếm tin nhắn trong một cuộc hội thoại.
    /// </summary>
    [HttpGet("{conversationId:int}/messages/search")]
    [Tags("Conversations.Messaging & Files")]
    [Authorize(Policy = "IsConversationMember")]
    public async Task<IActionResult> SearchMessages(int conversationId, [FromQuery] SearchMessagesQuery query)
    {
        var result = await _messageService.SearchMessagesAsync(conversationId, query);
        return Ok(result);
    }
    /// <summary>
    /// Lấy một "lát cắt" của cuộc hội thoại xung quanh một tin nhắn cụ thể.
    /// </summary>
    [HttpGet("{conversationId:int}/messages/context")]
    [Tags("Chat.Messages")]
    [Authorize(Policy = "IsConversationMember")]
    public async Task<IActionResult> GetMessageContext(int conversationId, [FromQuery] GetMessageContextQuery query)
    {
        var result = await _messageService.GetMessageContextAsync(conversationId, query);
        return Ok(result);
    }

    /// <summary>
    /// Bắt đầu một phiên gọi video mới trong cuộc hội thoại.
    /// </summary>
    /// <remarks>
    /// API này sẽ tạo một "phòng" trên LiveKit và trả về token cho người khởi tạo.
    /// Các thành viên khác sẽ nhận được thông báo real-time để tham gia.
    /// </remarks>
    /// <param name="conversationId">ID của cuộc hội thoại.</param>
    /// <response code="201">Tạo cuộc gọi thành công. Trả về session ID và LiveKit token.</response>
    /// <response code="403">Không có quyền bắt đầu cuộc gọi trong hội thoại này.</response>
    [HttpPost("{conversationId:int}/calls")]
    [Tags("Conversations.Video Calls")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(typeof(ApiResponse<StartCallResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartCallAsync([FromRoute] int conversationId)
    {
        var result = await _videoCallService.StartCallAsync(conversationId);
        return result.Success
            ? StatusCode(StatusCodes.Status201Created, result)
            : BadRequest(result);
    }
    /// <summary>
    /// Lấy lịch sử các cuộc gọi đã diễn ra trong cuộc hội thoại.
    /// </summary>
    /// <response code="200">Trả về danh sách lịch sử cuộc gọi.</response>
    /// <response code="403">Không có quyền xem lịch sử này.</response>
    [HttpGet("{conversationId:int}/call-history")]
    [Tags("Conversations.Video Calls")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CallHistoryItemDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCallHistoryAsync([FromRoute] int conversationId, [FromQuery] GetCallHistoryQuery query)
    {
        var result = await _videoCallService.GetCallHistoryAsync(conversationId, query);
        return Ok(result);
    }

    // === 4. Bình chọn (Polls) ===

    /// <summary>
    /// Tạo một cuộc bình chọn mới trong cuộc hội thoại.
    /// </summary>
    /// <remarks>
    /// Sau khi tạo, một tin nhắn hệ thống chứa cuộc bình chọn sẽ được gửi vào cuộc hội thoại.
    /// </remarks>
    /// <param name="conversationId">ID của cuộc hội thoại.</param>
    /// <param name="dto">Thông tin về cuộc bình chọn cần tạo (câu hỏi và các lựa chọn).</param>
    /// <response code="201">Tạo bình chọn thành công. Trả về thông tin chi tiết của bình chọn đã tạo.</response>
    /// <response code="400">Dữ liệu không hợp lệ (ví dụ: thiếu câu hỏi hoặc có ít hơn 2 lựa chọn).</response>
    /// <response code="403">Không có quyền tạo bình chọn trong hội thoại này.</response>
    [HttpPost("{conversationId:int}/polls")]
    [Tags("Conversations.Polls")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(typeof(ApiResponse<CreatePollResponseDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePollAsync([FromRoute] int conversationId, [FromBody] CreatePollDTO dto)
    {
        var result = await _pollService.CreatePollAsync(conversationId, dto);
        return result.Success
            ? StatusCode(StatusCodes.Status201Created, result)
            : BadRequest(result);
    }
    /// <summary>
    /// Lấy thông tin chi tiết và kết quả của một cuộc bình chọn.
    /// </summary>
    /// <param name="conversationId">ID của cuộc hội thoại (dùng để kiểm tra quyền).</param>
    /// <param name="pollId">ID của cuộc bình chọn cần xem.</param>
    /// <response code="200">Lấy thông tin thành công.</response>
    /// <response code="404">Không tìm thấy cuộc bình chọn.</response>
    /// <response code="403">Không có quyền xem cuộc bình chọn này.</response>
    [HttpGet("{conversationId:int}/polls/{pollId:int}")]
    [Tags("Conversations.Polls")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(typeof(ApiResponse<PollDetailDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPollDetailsAsync([FromRoute] int conversationId, [FromRoute] int pollId)
    {
        var result = await _pollService.GetPollDetailsAsync(pollId);
        return result.Success ? Ok(result) : NotFound(result);
    }
    /// <summary>
    /// Bỏ phiếu cho một lựa chọn trong cuộc bình chọn.
    /// </summary>
    /// <remarks>
    /// API này xử lý cả việc bỏ phiếu lần đầu, thay đổi lựa chọn, hoặc rút lại phiếu (bằng cách gửi `pollOptionId = null`).
    /// </remarks>
    /// <param name="conversationId">ID của cuộc hội thoại (dùng để kiểm tra quyền).</param>
    /// <param name="pollId">ID của cuộc bình chọn.</param>
    /// <param name="dto">Đối tượng chứa ID của lựa chọn. Gửi `null` để rút phiếu.</param>
    /// <response code="200">Bỏ phiếu thành công. Trả về kết quả bình chọn đã cập nhật.</response>
    /// <response code="400">Lựa chọn không hợp lệ hoặc bình chọn đã bị đóng.</response>
    [HttpPost("{conversationId:int}/polls/{pollId:int}/vote")]
    [Tags("Conversations.Polls")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CastVoteAsync([FromRoute] int conversationId, [FromRoute] int pollId, [FromBody] CastVoteDTO dto)
    {
        var result = await _pollService.CastVoteAsync(pollId, dto.PollOptionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Đóng một cuộc bình chọn (chỉ người tạo mới có thể thực hiện).
    /// </summary>
    /// <response code="204">Đóng bình chọn thành công.</response>
    /// <response code="403">Không có quyền đóng cuộc bình chọn này.</response>
    [HttpPost("{conversationId:int}/polls/{pollId:int}/close")]
    [Tags("Conversations.Polls")]
    [Authorize(Policy = "IsConversationMember")] // Service sẽ check quyền creator
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ClosePollAsync([FromRoute] int conversationId, [FromRoute] int pollId)
    {
        var result = await _pollService.ClosePollAsync(pollId);
        return result.Success ? NoContent() : Forbid();
    }
    /// <summary>
    /// Xóa một cuộc bình chọn (chỉ người tạo mới có thể thực hiện).
    /// </summary>
    /// <response code="204">Xóa bình chọn thành công.</response>
    /// <response code="403">Không có quyền xóa cuộc bình chọn này.</response>
    [HttpDelete("{conversationId:int}/polls/{pollId:int}")]
    [Tags("Conversations.Polls")]
    [Authorize(Policy = "IsConversationMember")] // Service sẽ check quyền creator
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePollAsync([FromRoute] int conversationId, [FromRoute] int pollId)
    {
        var result = await _pollService.DeletePollAsync(pollId);
        return result.Success ? NoContent() : Forbid();
    }
}
