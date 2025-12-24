using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class GetUserGroupsQuery : PaginationParams
{
    public string SearchTerm { get; set; } = string.Empty;
    public MyGroupFilterType FilterType { get; set; } = MyGroupFilterType.All;
}
