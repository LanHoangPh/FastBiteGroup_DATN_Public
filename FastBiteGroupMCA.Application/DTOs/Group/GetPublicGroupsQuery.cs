using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class GetPublicGroupsQuery : PaginationParams
{
    public string? SearchTerm { get; set; }
    public MyGroupFilterType? FilterType { get; set; } = MyGroupFilterType.All;
}
