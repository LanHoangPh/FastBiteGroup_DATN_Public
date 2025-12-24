using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Conversation;

public class GetMyConversationsQuery : PaginationParams
{
    public string? Filter { get; set; } // "direct" hoặc "group"
    // --- BỔ SUNG THUỘC TÍNH TÌM KIẾM ---
    public string? SearchTerm { get; set; }
}
