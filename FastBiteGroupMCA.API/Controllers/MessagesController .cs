using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/conversations/{conversationId:int}/messages")]
[Produces("application/json")]
[Authorize]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    public MessagesController(IMessageService messageService) { _messageService = messageService; }

    /// <summary>
    /// Thêm hoặc xóa một biểu cảm (reaction) khỏi tin nhắn.
    /// </summary>
    [HttpPost("{messageId}/toggle-reaction")]
    [Tags("Chat.Messages")]
    [Authorize(Policy = "IsConversationMember")] // Đảm bảo an toàn
    public async Task<IActionResult> ToggleReaction(int conversationId, string messageId, [FromBody] ToggleReactionDto dto)
    {
        var response = await _messageService.ToggleReactionAsync(conversationId, messageId, dto);

        if (!response.Success)
        {
            return response.Errors?.FirstOrDefault()?.ErrorCode switch
            {
                "MESSAGE_NOT_FOUND" => NotFound(response),
                "FORBIDDEN" => Forbid(),
                _ => BadRequest(response)
            };
        }

        return Ok(response);
    }
    /// <summary>
    /// Thu hồi (xóa) một tin nhắn.
    /// </summary>
    /// <param name="conversationId">ID của cuộc trò chuyện chứa tin nhắn.</param>
    /// <param name="messageId">ID của tin nhắn cần thu hồi.</param>
    [HttpDelete("{messageId}")]
    [Tags("Chat.Messages")]
    [Authorize(Policy = "IsConversationMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMessage(int conversationId, string messageId)
    {
        var response = await _messageService.DeleteMessageAsync(conversationId, messageId);

        if (!response.Success)
        {
            return response.Errors?.FirstOrDefault()?.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(response),
                "FORBIDDEN" => Forbid(),
                _ => BadRequest(response)
            };
        }

        return NoContent(); // Trả về 204 No Content khi xóa thành công
    }
}
