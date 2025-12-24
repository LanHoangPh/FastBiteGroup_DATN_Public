namespace FastBiteGroupMCA.Application.DTOs.Group.Admin;

public class ModerationOverviewDTO
{
    public int PendingReportsCount { get; set; }
    public int ResolvedReportsThisWeekCount { get; set; }
    public string AverageResponseTime { get; set; } = "N/A";
}
