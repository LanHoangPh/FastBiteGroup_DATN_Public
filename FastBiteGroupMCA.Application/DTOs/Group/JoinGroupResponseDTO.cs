namespace FastBiteGroupMCA.Application.DTOs.Group;

public class JoinGroupResponseDTO
{
    public Guid GroupId { get; set; }
    public string? GroupName { get; set; }
    /// <summary>
    /// ID của cuộc trò chuyện mặc định (#general) thuộc về nhóm đó.
    /// </summary>
    public int DefaultConversationId { get; set; }
}
