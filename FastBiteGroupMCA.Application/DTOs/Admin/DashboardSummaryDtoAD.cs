//namespace FastBiteGroupMCA.Application.DTOs.Admin;
///// <summary>
///// Dữ liệu tổng hợp cho trang dashboard của Admin.
///// </summary>
//public class DashboardSummaryDtoAD
//{
//    /// <summary>
//    /// Các chỉ số thống kê quan trọng.
//    /// </summary>
//    public KeyStatsDto KeyStats { get; set; } = new();
//    public ModerationStatsDto ModerationStats { get; set; } = new();
//    public EngagementStatsDto EngagementStats { get; set; } = new();

//    /// <summary>
//    /// Dữ liệu cho biểu đồ tăng trưởng người dùng trong 30 ngày qua.
//    /// </summary>
//    public List<UserGrowthItemDto> UserGrowthChartData { get; set; } = new();

//    public List<GroupGrowthItemDto> GroupGrowthChartData { get; set; } = new();
//    /// <summary>
//    /// Danh sách 5 người dùng mới nhất.
//    /// </summary>
//    public List<RecentUserDto> RecentUsers { get; set; } = new();

//    /// <summary>
//    /// Danh sách 5 nhóm mới được tạo gần đây nhất.
//    /// </summary>
//    public List<RecentGroupDto> RecentGroups { get; set; } = new();

//    public List<VideoCallChartItemDto> VideoCallChartData { get; set; } = new();
//}
//public class VideoCallChartItemDto
//{
//    public string Date { get; set; } = string.Empty; // yyyy-MM-dd
//    public int NewCallCount { get; set; }
//}
//public class GroupGrowthItemDto
//{
//    public string Date { get; set; } = string.Empty; // yyyy-MM-dd
//    public int NewGroupCount { get; set; }
//}
//public class KeyStatsDto
//{
//    public int TotalUsers { get; set; }
//    public int NewUsersLast7Days { get; set; }
//    public int TotalGroups { get; set; }
//    public int TotalPosts { get; set; }
//    public int TotalVideoCalls { get; set; }
//}

//public class UserGrowthItemDto
//{
//    public string Date { get; set; } = string.Empty; // yyyy-MM-dd
//    public int NewUserCount { get; set; }
//}

//public class RecentUserDto
//{
//    public Guid UserId { get; set; }
//    public string FullName { get; set; } = string.Empty;
//    public DateTime CreatedAt { get; set; }
//}

//public class RecentGroupDto
//{
//    public Guid GroupId { get; set; }
//    public string GroupName { get; set; } = string.Empty;
//    public DateTime CreatedAt { get; set; }
//}
//// DTO con cho các chỉ số kiểm duyệt
//public class ModerationStatsDto
//{
//    public int PendingReportsCount { get; set; }
//    public int DeactivatedUsersCount { get; set; }
//}

//// DTO con cho các chỉ số tương tác
//public class EngagementStatsDto
//{
//    public int ActiveUsersLast7Days { get; set; }
//    public List<TopGroupDto> TopActiveGroups { get; set; } = new();
//}
//public class TopGroupDto
//{
//    public Guid GroupId { get; set; }
//    public string GroupName { get; set; } = string.Empty;
//    public string? GroupAvatarUrl { get; set; }

//    /// <summary>
//    /// Chỉ số hoạt động (ví dụ: tổng số bài viết + bình luận mới).
//    /// </summary>
//    public int ActivityCount { get; set; }
//}