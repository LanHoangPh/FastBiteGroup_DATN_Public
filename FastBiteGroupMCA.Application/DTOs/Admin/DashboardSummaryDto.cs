using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin;

public class DashboardSummaryDto
{
    public KeyStatsDto1 KeyStats { get; set; } = new();
    public ModerationStatsDto1 ModerationStats { get; set; } = new();
    public List<RecentUserDto1> RecentUsers { get; set; } = new();
    public List<RecentGroupDto1> RecentGroups { get; set; } = new();
    public List<RecentReportDto> RecentPendingReports { get; set; } = new();
    public List<RecentAuditLogDto> RecentAdminActions { get; set; } = new();
}
// DTO cho các chỉ số chính
public class KeyStatsDto1
{
    public int TotalUsers { get; set; }
    public int NewUsersLast7Days { get; set; }
    public int ActiveUsersLast7Days { get; set; }
    public int TotalGroups { get; set; }
    public int TotalPosts { get; set; }
    public int TotalVideoCalls { get; set; }
}

// DTO cho các chỉ số kiểm duyệt
public class ModerationStatsDto1
{
    public int PendingReportsCount { get; set; }
    public int DeactivatedUsersCount { get; set; }
}

// DTO cho user mới
public class RecentUserDto1
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// DTO cho group mới
public class RecentGroupDto1
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
public class RecentReportDto
{
    public int ReportId { get; set; }
    public EnumReportedContentType ContentType { get; set; } // Post hay Comment
    public string Reason { get; set; } = string.Empty;
    public string ReportedByUser { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string UrlToContent { get; set; } = string.Empty; // Link để admin click vào xem
}

// DTO mới cho một log hoạt động của admin
public class RecentAuditLogDto
{
    public string AdminFullName { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
