using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class GetGroupMembersQuery : PaginationParams
{
    public string? SearchTerm { get; set; }
}
