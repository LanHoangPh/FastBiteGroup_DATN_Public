using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Conversation;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices;

public interface IConversationService
{
    Task<ApiResponse<ConversationResponseDto>> FindOrCreateDirectConversationAsync(Guid partnerUserId);
    Task<ApiResponse<PagedResult<ConversationListItemDTO>>> GetMyConversationsAsync(GetMyConversationsQuery query);
    Task<ApiResponse<ConversationDetailDto>> GetConversationDetailsAsync(int conversationId, GetConversationMessagesQuery query);
    Task<ApiResponse<object>> DeleteConversationForCurrentUserAsync(int conversationId);
    // ... các phương thức khác ...

    /// <summary>
    /// (Dùng nội bộ) Xây dựng DTO hiển thị danh sách cho một cuộc trò chuyện cụ thể và một người dùng cụ thể.
    /// </summary>
    Task<ConversationListItemDTO?> BuildConversationListItemDtoForUser(int conversationId, Guid userId);
}
