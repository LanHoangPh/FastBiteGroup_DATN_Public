using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Infastructure.Caching;
using System.Collections.Generic;

namespace FastBiteGroupMCA.Infastructure.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<AdminDashboardService> _logger;
    private const string CACHE_KEY = "ADMIN_DASHBOARD_SUMMARY";
    private static readonly string CacheKey = "AdminDashboardSummary";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AdminDashboardService(IUnitOfWork uow, ICacheService cache, ILogger<AdminDashboardService> logger)
    {
        _unitOfWork = uow;
        _cache = cache;
        _logger = logger;
    }

    //public async Task<DashboardSummaryDtoAD> GetDashboardSummaryAsync()
    //{
    //    _logger.LogInformation("Fetching admin dashboard summary data");
    //    var cachedData = await _cache.GetAsync<DashboardSummaryDtoAD>(CACHE_KEY);
    //    if (cachedData != null)
    //    {
    //        return cachedData;
    //    }

    //    try
    //    {
    //        var now = DateTime.UtcNow;
    //        var sevenDaysAgo = now.AddDays(-7);
    //        var thirtyDaysAgo = now.AddDays(-30);

    //        var totalUsers = await _unitOfWork.Users.GetQueryable().CountAsync(u => !u.IsDeleted);
    //        var newUsers = await _unitOfWork.Users.GetQueryable().CountAsync(u => !u.IsDeleted && u.CreatedAt >= sevenDaysAgo);
    //        var totalGroups = await _unitOfWork.Groups.GetQueryable().CountAsync(g => !g.IsDeleted);
    //        var totalPosts = await _unitOfWork.Posts.GetQueryable().CountAsync(p => !p.IsDeleted);

    //        var totalVideoCalls = await _unitOfWork.VideoCallSessions.GetQueryable().CountAsync();

    //        var pendingReports = await _unitOfWork.ContentReports.GetQueryable()
    //        .CountAsync(r => r.Status == EnumContentReportStatus.Pending);

    //        var deactivatedUsers = await _unitOfWork.Users.GetQueryable()
    //            .CountAsync(u => !u.IsActive && !u.IsDeleted);

    //        var activeUsers = await _unitOfWork.RefreshToken.GetQueryable()
    //            .Where(rt => !rt.IsRevoked && rt.CreatedAt >= sevenDaysAgo)
    //            .Select(rt => rt.UserId).Distinct().CountAsync();

    //        var growthRaw = await _unitOfWork.Users.GetQueryable()
    //            .Where(u => !u.IsDeleted && u.CreatedAt >= thirtyDaysAgo)
    //            .GroupBy(u => u.CreatedAt.Date)
    //            .Select(g => new { Date = g.Key, Count = g.Count() })
    //            .ToListAsync();

    //        var groupGrowthRaw = await _unitOfWork.Groups.GetQueryable()
    //        .Where(g => !g.IsDeleted && g.CreatedAt >= thirtyDaysAgo)
    //        .GroupBy(g => g.CreatedAt.Date)
    //        .Select(g => new { Date = g.Key, Count = g.Count() })
    //        .ToListAsync();


    //        // 2. Lấy dữ liệu cho biểu đồ
    //        var callGrowthRaw = await _unitOfWork.VideoCallSessions.GetQueryable()
    //            .Where(v => v.StartedAt >= thirtyDaysAgo)
    //            .GroupBy(v => v.StartedAt.Date)
    //            .Select(g => new { Date = g.Key, Count = g.Count() })
    //            .ToListAsync();


    //        var recentUsers = await _unitOfWork.Users.GetQueryable()
    //            .Where(u => !u.IsDeleted)
    //            .OrderByDescending(u => u.CreatedAt)
    //            .Take(5)
    //            .Select(u => new RecentUserDto
    //            {
    //                UserId = u.Id,
    //                FullName = u.FullName ?? (u.FisrtName + " " + u.LastName),
    //                CreatedAt = u.CreatedAt
    //            }).ToListAsync();

    //        var recentGroups = await _unitOfWork.Groups.GetQueryable()
    //            .Where(g => !g.IsDeleted)
    //            .OrderByDescending(g => g.CreatedAt)
    //            .Take(5)
    //            .Select(g => new RecentGroupDto
    //            {
    //                GroupId = g.GroupID,
    //                GroupName = g.GroupName,
    //                CreatedAt = g.CreatedAt
    //            }).ToListAsync();

    //        var postCounts = await _unitOfWork.Posts.GetQueryable()
    //        .Where(p => !p.IsDeleted && p.CreatedAt >= thirtyDaysAgo)
    //        .GroupBy(p => p.GroupID)
    //        .Select(g => new { GroupId = g.Key, Count = g.Count() })
    //        .ToListAsync();

    //        // 2. Đếm số bình luận mới trong mỗi nhóm
    //        var commentCounts = await _unitOfWork.PostComments.GetQueryable()
    //            .Where(c => !c.IsDeleted && c.CreatedAt >= thirtyDaysAgo)
    //            .GroupBy(c => c.Post!.GroupID)
    //            .Select(g => new { GroupId = g.Key, Count = g.Count() })
    //            .ToListAsync();

    //        var groupActivityScores = new Dictionary<Guid, int>();

    //        foreach (var item in postCounts)
    //        {
    //            groupActivityScores[item.GroupId] = groupActivityScores.GetValueOrDefault(item.GroupId, 0) + item.Count;
    //        }
    //        foreach (var item in commentCounts)
    //        {
    //            groupActivityScores[item.GroupId] = groupActivityScores.GetValueOrDefault(item.GroupId, 0) + item.Count;
    //        }

    //        var top5GroupIds = groupActivityScores
    //        .OrderByDescending(pair => pair.Value)
    //        .Take(5)
    //        .Select(pair => pair.Key)
    //        .ToList();

    //        var top5ActiveGroups = new List<TopGroupDto>();
    //        if (top5GroupIds.Any())
    //        {
    //            var topGroupsDetails = await _unitOfWork.Groups.GetQueryable()
    //                .Where(g => top5GroupIds.Contains(g.GroupID))
    //                .Select(g => new { g.GroupID, g.GroupName, g.GroupAvatarUrl })
    //                .ToListAsync();

    //            top5ActiveGroups = topGroupsDetails.Select(g => new TopGroupDto
    //            {
    //                GroupId = g.GroupID,
    //                GroupName = g.GroupName,
    //                GroupAvatarUrl = g.GroupAvatarUrl,
    //                ActivityCount = groupActivityScores[g.GroupID] // Lấy điểm từ Dictionary
    //            })
    //            .OrderByDescending(g => g.ActivityCount) // Sắp xếp lại lần cuối
    //            .ToList();
    //        }
    //        // lấy biuể đồ user
    //        var growthDict = growthRaw.ToDictionary(x => x.Date, x => x.Count);
    //        var growthData = new List<UserGrowthItemDto>();
    //        for (int i = 0; i < 30; i++)
    //        {
    //            var date = thirtyDaysAgo.Date.AddDays(i);
    //            growthData.Add(new UserGrowthItemDto
    //            {
    //                Date = date.ToString("yyyy-MM-dd"),
    //                NewUserCount = growthDict.TryGetValue(date, out var c) ? c : 0
    //            });
    //        }
    //        // lấy biểu đồ group
    //        var groupGrowthDict = groupGrowthRaw.ToDictionary(x => x.Date, x => x.Count);
    //        var groupGrowthData = new List<GroupGrowthItemDto>();
    //        for (int i = 0; i < 30; i++)
    //        {
    //            var date = thirtyDaysAgo.Date.AddDays(i);
    //            groupGrowthData.Add(new GroupGrowthItemDto
    //            {
    //                Date = date.ToString("yyyy-MM-dd"),
    //                NewGroupCount = groupGrowthDict.TryGetValue(date, out var c) ? c : 0
    //            });
    //        }

    //        // lấy biểu đồ video call
    //        var callGrowthDict = callGrowthRaw.ToDictionary(x => x.Date, x => x.Count);
    //        var callGrowthData = new List<VideoCallChartItemDto>();
    //        for (int i = 0; i < 30; i++)
    //        {
    //            var date = thirtyDaysAgo.Date.AddDays(i);
    //            callGrowthData.Add(new VideoCallChartItemDto
    //            {
    //                Date = date.ToString("yyyy-MM-dd"),
    //                NewCallCount = callGrowthDict.TryGetValue(date, out var c) ? c : 0
    //            });
    //        }

    //        var summary = new DashboardSummaryDtoAD
    //        {
    //            KeyStats = new KeyStatsDto
    //            {
    //                TotalUsers = totalUsers,
    //                NewUsersLast7Days = newUsers,
    //                TotalGroups = totalGroups,
    //                TotalPosts = totalPosts,
    //                TotalVideoCalls = totalVideoCalls
    //            },
    //            ModerationStats = new ModerationStatsDto
    //            {
    //                PendingReportsCount = pendingReports,
    //                DeactivatedUsersCount = deactivatedUsers
    //            },
    //            EngagementStats = new EngagementStatsDto
    //            {
    //                ActiveUsersLast7Days = activeUsers,
    //                TopActiveGroups = top5ActiveGroups
    //            },
    //            UserGrowthChartData = growthData,
    //            GroupGrowthChartData = groupGrowthData,
    //            RecentUsers = recentUsers,
    //            RecentGroups = recentGroups,
    //            VideoCallChartData = callGrowthData,
    //        };

    //        await _cache.SetAsync(CACHE_KEY, summary, CacheDuration);

    //        return summary;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error generating admin dashboard summary");
    //        throw;
    //    }
    //}



    public async Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryADAsync()
    {
        var cachedData = await _cache.GetAsync<DashboardSummaryDto>(CacheKey);
        if (cachedData != null)
            return ApiResponse<DashboardSummaryDto>.Ok(cachedData);

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Chạy các truy vấn song song để lấy dữ liệu
        var totalUsers = await _unitOfWork.Users.GetQueryable().CountAsync(u => !u.IsDeleted);
        var newUsers = await _unitOfWork.Users.GetQueryable().CountAsync(u => !u.IsDeleted && u.CreatedAt >= sevenDaysAgo);
        var activeUsers = await _unitOfWork.RefreshToken.GetQueryable()
            .Where(rt => !rt.IsRevoked && rt.CreatedAt >= sevenDaysAgo)
            .Select(rt => rt.UserId).Distinct().CountAsync();
        var totalGroups = await _unitOfWork.Groups.GetQueryable().CountAsync(g => !g.IsDeleted);
        var totalPosts = await _unitOfWork.Posts.GetQueryable().CountAsync(p => !p.IsDeleted);
        var totalVideoCalls = await _unitOfWork.VideoCallSessions.GetQueryable().CountAsync();
        var pendingReports = await _unitOfWork.ContentReports.GetQueryable().CountAsync(r => r.Status == EnumContentReportStatus.Pending);
        var deactivatedUsers = await _unitOfWork.Users.GetQueryable().CountAsync(u => !u.IsActive && !u.IsDeleted);
        var recentUsers = await _unitOfWork.Users.GetQueryable().Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt).Take(5)
            .Select(u => new RecentUserDto1 {
                UserId = u.Id,
                FullName = u.FullName ?? (u.FisrtName + " " + u.LastName),
                CreatedAt = u.CreatedAt
            }).ToListAsync();
        var recentGroups = await _unitOfWork.Groups.GetQueryable().Where(g => !g.IsDeleted)
            .OrderByDescending(g => g.CreatedAt).Take(5)
            .Select(g => new RecentGroupDto1 {
                GroupId = g.GroupID,
                GroupName = g.GroupName,
                CreatedAt = g.CreatedAt
            }).ToListAsync();

        var recentPendingReports = await _unitOfWork.ContentReports.GetQueryable()
            .Where(r => r.Status == EnumContentReportStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Include(r => r.ReportedByUser)
            .Include(r => r.Group)
            .Select(r => new RecentReportDto
            {
                ReportId = r.ContentReportID,
                ContentType = r.ReportedContentType,
                Reason = r.Reason,
                ReportedByUser = r.ReportedByUser.FullName ?? "N/A",
                ReportedAt = r.CreatedAt,
                GroupName = r.Group.GroupName,
                UrlToContent = r.ReportedContentType == EnumReportedContentType.Post
                    ? $"/admin/posts/{r.ReportedContentID}" // Link tới trang chi tiết post của admin
                    : $"/admin/posts/{r.ReportedContentID}" // Comment cũng cần PostId, cần điều chỉnh logic lấy PostId nếu cần
            })
            .ToListAsync();

        // --- BỔ SUNG: LẤY 5 HÀNH ĐỘNG ADMIN GẦN ĐÂY NHẤT ---
        var recentAdminActions = await _unitOfWork.AdminAuditLogs.GetQueryable()
            .OrderByDescending(log => log.Timestamp)
            .Take(5)
            .Select(log => new RecentAuditLogDto
            {
                AdminFullName = log.AdminFullName,
                ActionDescription = $"{log.ActionType} on {log.TargetEntityType} (ID: {log.TargetEntityId})",
                Timestamp = log.Timestamp
            })
            .ToListAsync();


        var summary = new DashboardSummaryDto
        {
            KeyStats = new KeyStatsDto1
            {
                TotalUsers = totalUsers,
                NewUsersLast7Days =  newUsers,
                ActiveUsersLast7Days =  activeUsers,
                TotalGroups =  totalGroups,
                TotalPosts =  totalPosts,
                TotalVideoCalls =   totalVideoCalls
            },
            ModerationStats = new ModerationStatsDto1
            {
                PendingReportsCount =  pendingReports,
                DeactivatedUsersCount = deactivatedUsers
            },
            RecentUsers = recentUsers,
            RecentGroups = recentGroups,
            RecentPendingReports = recentPendingReports,
            RecentAdminActions = recentAdminActions
        };

        await _cache.SetAsync(CacheKey, summary, CacheDuration);
        return ApiResponse<DashboardSummaryDto>.Ok(summary);
    }

    public async Task<ApiResponse<AnalyticsDto>> GetAnalyticsDataAsync(GetAnalyticsDataRequest request)
    {
        var cacheKey = $"AdminAnalyticsData_{request.TimeRange}";
        var cachedData = await _cache.GetAsync<AnalyticsDto>(cacheKey);
        if (cachedData != null)
            return ApiResponse<AnalyticsDto>.Ok(cachedData);

        try
        {
            var now = DateTime.UtcNow;
            var startDate = now.Date.AddDays(-30);

            bool groupByMonth = false;

            int periodInMonths = 0;
            int periodInDays = 30;

            switch (request.TimeRange)
            {
                case EnumTimeRange.Last7Days:
                    startDate = now.Date.AddDays(-7);
                    periodInDays = 7;
                    break;
                case EnumTimeRange.Last6Months:
                    startDate = now.Date.AddMonths(-6);
                    groupByMonth = true;
                    periodInMonths = 6;
                    break;
                case EnumTimeRange.Last12Months:
                    startDate = now.Date.AddYears(-1);
                    groupByMonth = true;
                    periodInMonths = 12;
                    break;
                case EnumTimeRange.Last30Days:
                default:
                    break;
            }


            List<ChartDataItemDto> userGrowthData;
            List<ChartDataItemDto> groupGrowthData;
            List<ChartDataItemDto> callGrowthData;
            List<ChartDataItemDto> postGrowthData;
            List<ChartDataItemDto> commentGrowthData;

            if (groupByMonth)
            {
                // --- LOGIC GROUP THEO THÁNG CHO TẤT CẢ BIỂU ĐỒ ---
                var users = await _unitOfWork.Users.GetQueryable().Where(u => !u.IsDeleted && u.CreatedAt >= startDate)
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();

                var groups = await _unitOfWork.Groups.GetQueryable().Where(g => !g.IsDeleted && g.CreatedAt >= startDate)
                    .GroupBy(g => new { g.CreatedAt.Year, g.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();

                var calls = await _unitOfWork.VideoCallSessions.GetQueryable().Where(v => v.StartedAt >= startDate)
                    .GroupBy(v => new { v.StartedAt.Year, v.StartedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();

                var posts = await _unitOfWork.Posts.GetQueryable().Where(p => !p.IsDeleted && p.CreatedAt >= startDate)
                    .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();

                var comments = await _unitOfWork.PostComments.GetQueryable().Where(c => !c.IsDeleted && c.CreatedAt >= startDate)
                    .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();


                userGrowthData = ProcessMonthlyChartData(users, item => item.Year, item => item.Month, item => item.Count, now, periodInMonths);
                groupGrowthData = ProcessMonthlyChartData(groups, item => item.Year, item => item.Month, item => item.Count, now, periodInMonths);
                callGrowthData = ProcessMonthlyChartData(calls, item => item.Year, item => item.Month, item => item.Count, now, periodInMonths);
                postGrowthData = ProcessMonthlyChartData(posts, item => item.Year, item => item.Month, item => item.Count, now, periodInMonths);
                commentGrowthData = ProcessMonthlyChartData(comments, item => item.Year, item => item.Month, item => item.Count, now, periodInMonths);
            }
            else
            {
                // --- LOGIC GROUP THEO NGÀY CHO TẤT CẢ BIỂU ĐỒ ---
                var users = await _unitOfWork.Users.GetQueryable().Where(u => !u.IsDeleted && u.CreatedAt >= startDate)
                    .GroupBy(u => u.CreatedAt.Date).Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();

                var groups = await _unitOfWork.Groups.GetQueryable().Where(g => !g.IsDeleted && g.CreatedAt >= startDate)
                    .GroupBy(g => g.CreatedAt.Date).Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();

                var calls = await _unitOfWork.VideoCallSessions.GetQueryable().Where(v => v.StartedAt >= startDate).
                    GroupBy(v => v.StartedAt.Date).Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();

                var posts = await _unitOfWork.Posts.GetQueryable().Where(p => !p.IsDeleted && p.CreatedAt >= startDate)
                    .GroupBy(p => p.CreatedAt.Date).Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();

                var comments = await _unitOfWork.PostComments.GetQueryable().Where(c => !c.IsDeleted && c.CreatedAt >= startDate)
                    .GroupBy(c => c.CreatedAt.Date).Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();

                userGrowthData = ProcessDailyChartData(users, item => item.Date, item => item.Count, startDate, periodInDays);
                groupGrowthData = ProcessDailyChartData(groups, item => item.Date, item => item.Count, startDate, periodInDays);
                callGrowthData = ProcessDailyChartData( calls, item => item.Date, item => item.Count, startDate, periodInDays);
                postGrowthData = ProcessDailyChartData( posts, item => item.Date, item => item.Count, startDate, periodInDays);
                commentGrowthData = ProcessDailyChartData(comments, item => item.Date, item => item.Count, startDate, periodInDays);
            }

            // --- TÍNH TOÁN DỮ LIỆU CHO CÁC BIỂU ĐỒ PHÂN LOẠI ---
            var userRoleDistribution = await _unitOfWork.UserRoles.GetQueryable()
                .Join(_unitOfWork.Roles.GetQueryable(), ur => ur.RoleId, r => r.Id, (ur, r) => new { r.Name })
                .GroupBy(x => x.Name)
                .Select(g => new ChartItemDto { Label = g.Key, Value = g.Count() }).ToListAsync();

            var userStatusDistribution = await _unitOfWork.Users.GetQueryable()
                .Where(u => !u.IsDeleted).GroupBy(u => u.IsActive)
                .Select(g => new ChartItemDto { Label = g.Key ? "Hoạt động" : "Bị vô hiệu hóa", Value = g.Count() }).ToListAsync();

            var groupTypeDistribution = await _unitOfWork.Groups.GetQueryable()
                .Where(g => !g.IsDeleted).GroupBy(g => g.GroupType)
                .Select(g => new ChartItemDto { Label = g.Key.ToString(), Value = g.Count() }).ToListAsync();

            var reportStatusDistribution = await _unitOfWork.ContentReports.GetQueryable()
                    .GroupBy(r => r.Status)
                    .Select(g => new ChartItemDto { Label = g.Key.ToString(), Value = g.Count() }).ToListAsync();

            var analyticsData = new AnalyticsDto
            {
                UserGrowthChartData = userGrowthData,
                GroupGrowthChartData = groupGrowthData,
                VideoCallChartData = callGrowthData,
                PostGrowthChartData = postGrowthData,
                CommentGrowthChartData = commentGrowthData,
                ClassificationCharts = new ClassificationChartsDto
                {
                    UserRoleDistribution =  userRoleDistribution,
                    UserStatusDistribution =  userStatusDistribution,
                    GroupTypeDistribution =  groupTypeDistribution,
                    ReportStatusDistribution = reportStatusDistribution
                }
            };

            await _cache.SetAsync(cacheKey, analyticsData, CacheDuration);
            return ApiResponse<AnalyticsDto>.Ok(analyticsData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating admin analytics data");
            return ApiResponse<AnalyticsDto>.Fail("ANALYTICS_ERROR", "Lỗi khi tạo dữ liệu thống kê.");
        }
    }

    private static List<ChartDataItemDto> ProcessDailyChartData<T>(
    IEnumerable<T> rawData,
    Func<T, DateTime> dateSelector,
    Func<T, int> countSelector,
    DateTime startDate,
    int daysInPeriod)
    {
        var dictionary = rawData.ToDictionary(
            keySelector: x => dateSelector(x).Date,
            elementSelector: x => countSelector(x)
        );

        var resultData = new List<ChartDataItemDto>();
        for (int i = 0; i < daysInPeriod; i++)
        {
            var date = startDate.Date.AddDays(i);
            resultData.Add(new ChartDataItemDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Count = dictionary.TryGetValue(date, out var count) ? count : 0
            });
        }
        return resultData;
    }

    /// <summary>
    /// Xử lý dữ liệu thô được gộp theo tháng và điền vào các tháng còn thiếu.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của dữ liệu thô (thường là anonymous type).</typeparam>
    /// <param name="rawData">Danh sách dữ liệu thô từ DB.</param>
    /// <param name="yearSelector">Hàm để lấy ra thuộc tính Year.</param>
    /// <param name="monthSelector">Hàm để lấy ra thuộc tính Month.</param>
    /// <param name="countSelector">Hàm để lấy ra thuộc tính Count.</param>
    /// <param name="endDate">Ngày kết thúc của khoảng thời gian.</param>
    /// <param name="monthsInPeriod">Số tháng trong khoảng thời gian (ví dụ: 6 hoặc 12).</param>
    /// <returns>Danh sách các điểm dữ liệu cho biểu đồ.</returns>
    private static List<ChartDataItemDto> ProcessMonthlyChartData<T>(
        IEnumerable<T> rawData,
        Func<T, int> yearSelector,
        Func<T, int> monthSelector,
        Func<T, int> countSelector,
        DateTime endDate,
        int monthsInPeriod)
    {
        // Tạo một Dictionary để tra cứu nhanh với key là (Năm, Tháng)
        var dictionary = rawData.ToDictionary(
            keySelector: x => (yearSelector(x), monthSelector(x)), // Key là một ValueTuple (Year, Month)
            elementSelector: x => countSelector(x)
        );

        var resultData = new List<ChartDataItemDto>();
        for (int i = 0; i < monthsInPeriod; i++)
        {
            // Lặp ngược từ tháng hiện tại về quá khứ
            var date = endDate.AddMonths(-i);
            var key = (date.Year, date.Month);

            resultData.Add(new ChartDataItemDto
            {
                Date = date.ToString("yyyy-MM"), // Label của biểu đồ sẽ là "2025-08"
                Count = dictionary.TryGetValue(key, out var count) ? count : 0
            });
        }

        // Sắp xếp lại để danh sách theo thứ tự thời gian tăng dần cho biểu đồ
        return resultData.OrderBy(d => d.Date).ToList();
    }
}
