using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class UserSearchForInviteRequest : PaginationParams
{
    public Guid GroupId { get; set; } // Nhóm đang muốn mời vào
    public string? Query { get; set; } // tìm kiếm
}
