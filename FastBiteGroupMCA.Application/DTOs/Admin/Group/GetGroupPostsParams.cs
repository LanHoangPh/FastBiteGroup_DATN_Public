using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class GetGroupPostsParams : PaginationParams
{
    public string? SearchTerm { get; set; }
}
