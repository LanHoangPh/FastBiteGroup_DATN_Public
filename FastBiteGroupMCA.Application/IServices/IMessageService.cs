using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Domain.Entities;
using System.ComponentModel;

namespace FastBiteGroupMCA.Application.IServices;

public interface IMessageService
{
    Task<ApiResponse<PagedResult<MessageDTO>>> SearchMessagesAsync(int conversationId, SearchMessagesQuery query);
    Task<ApiResponse<MessageContextResponseDto>> GetMessageContextAsync(int conversationId, GetMessageContextQuery query);


    Task<ApiResponse<object>> DeleteMessageAsync(int conversationId, string messageId);
    Task<ApiResponse<MessageHistoryResponseDTO>> GetMessageHistoryAsync(int conversationId, GetMessagesQuery query);
    Task<ApiResponse<MessageDTO>> SendMessageAsync(int conversationId, SendMessageDTO dto);
    /// <summary>
    /// Gửi một tin nhắn hệ thống vào một cuộc trò chuyện.
    /// </summary>
    /// <param name="conversationId">ID của cuộc trò chuyện.</param>
    /// <param name="content">Nội dung tin nhắn.</param>
    Task SendSystemMessageAsync(int conversationId, string content);
    [DisplayName("Process Side Effects for new message: {0}")]
    Task ProcessNewMessageSideEffectsAsync(string messageId, int conversationId);
    
    /// <summary>
    // Phải là public để Hangfire có thể gọi
    /// </summary>
    [DisplayName("Broadcast Message Deletion for Conversation: {0}")]
    Task BroadcastMessageDeletionAsync(int conversationId, string messageId);
    Task<ApiResponse<ToggleReactionResponseDto>> ToggleReactionAsync(int conversationId, string messageId, ToggleReactionDto dto);

    // Phương thức public cho Hangfire
    [DisplayName("Broadcast Reaction Update for Message: {1}")]
    Task BroadcastReactionUpdateAsync(int conversationId, string messageId, List<Reaction> newReactions);
}
