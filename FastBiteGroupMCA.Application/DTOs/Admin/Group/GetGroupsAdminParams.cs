using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class GetGroupsAdminParams : PaginationParams
{
    public string? SearchTerm { get; set; }
    public MyGroupFilterType? GroupType { get; set; }
    public GroupStatusFilter Status { get; set; } = GroupStatusFilter.Active;
    /// <summary>
    /// Độ lệch múi giờ của client so với UTC, tính bằng phút.
    /// Ví dụ: Việt Nam (UTC+7) sẽ là -420.
    /// </summary>
    public int? TimezoneOffsetMinutes { get; set; }
    // Có thể thêm các tham số sắp xếp ở đây trong tương lai
    // public string SortBy { get; set; } = "LastActivityAt";
}
