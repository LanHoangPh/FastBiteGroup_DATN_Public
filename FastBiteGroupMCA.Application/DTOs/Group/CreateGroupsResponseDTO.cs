namespace FastBiteGroupMCA.Application.DTOs.Group;

public class CreateGroupsResponseDTO
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int DefaultConversationId { get; set; } // sẽ luôn bằng ko nếu là nhóm cộng đồng vì ko có cuộc trò chuyện
}
