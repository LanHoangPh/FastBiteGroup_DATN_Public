namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class GroupStatsDTO
{
    public int MemberCount { get; set; }
    public int PostCount { get; set; }
    public int PendingReportsCount { get; set; } // THÊM: Số báo cáo đang chờ
    public DateTime? LastActivityAt { get; set; } // THÊM: Lần hoạt động cuối
}
