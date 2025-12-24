using FastBiteGroupMCA.Application.DTOs.Group.Admin;

namespace FastBiteGroupMCA.Infastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IUnitOfWork unitOfWork, ICurrentUser currentUser, ILogger<DashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Lấy dữ liệu thống kê tổng quan cho dashboard quản lý nhóm.
    /// </summary>
    public async Task<ApiResponse<GroupDashboardDTO>> GetGroupDashboardStatsAsync(Guid groupId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<GroupDashboardDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.", 401);

        // BƯỚC 1: KIỂM TRA QUYỀN TRUY CẬP (Yêu cầu là Admin hoặc Mod)
        var isAuthorized = await IsUserAdminOrMod(groupId, currentUserId);
        if (!isAuthorized)
        {
            return ApiResponse<GroupDashboardDTO>.Fail("Forbidden", "Bạn không có quyền truy cập dashboard của nhóm này.", 403);
        }

        try
        {
            // BƯỚC 2: ĐỊNH NGHĨA CÁC MỐC THỜI GIAN CẦN THIẾT
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfPreviousMonth = startOfThisMonth.AddMonths(-1);

            // BƯỚC 3: TRUY VẤN TẤT CẢ DỮ LIỆU CẦN THIẾT MỘT CÁCH SONG SONG
            // Điều này giúp tăng hiệu năng bằng cách không phải chờ từng query hoàn thành

            // --- Nhóm query về Thành viên ---
            var currentMemberCountTask = _unitOfWork.GroupMembers.GetQueryable().CountAsync(gm => gm.GroupID == groupId);
            var membersJoinedThisMonthTask = _unitOfWork.GroupMembers.GetQueryable().CountAsync(gm => gm.GroupID == groupId && gm.JoinedAt >= startOfThisMonth);
            var membersJoinedPreviousMonthTask = _unitOfWork.GroupMembers.GetQueryable().CountAsync(gm => gm.GroupID == groupId && gm.JoinedAt >= startOfPreviousMonth && gm.JoinedAt < startOfThisMonth);

            // --- Nhóm query về Bài viết & Tương tác ---
            var postsThisWeekTask = _unitOfWork.Posts.GetQueryable().CountAsync(p => p.GroupID == groupId && p.CreatedAt >= sevenDaysAgo);
            var interactionsThisWeekTask = GetInteractionCountAsync(groupId, sevenDaysAgo);

            // --- Nhóm query về Kiểm duyệt ---
            var pendingReportsCountTask = _unitOfWork.ContentReports.GetQueryable().CountAsync(r => r.GroupID == groupId && r.Status == EnumContentReportStatus.Pending);

            // --- Nhóm query về Hoạt động ---
            var activeMemberCountTask = GetActiveMemberCountAsync(groupId, sevenDaysAgo);

            // Chờ tất cả các tác vụ truy vấn hoàn thành
            await Task.WhenAll(
                currentMemberCountTask, membersJoinedThisMonthTask, membersJoinedPreviousMonthTask,
                postsThisWeekTask, interactionsThisWeekTask,
                pendingReportsCountTask, activeMemberCountTask
            );

            // BƯỚC 4: LẤY KẾT QUẢ VÀ TẠO DTO TRẢ VỀ
            var currentMemberCount = await currentMemberCountTask;
            var membersJoinedThisMonth = await membersJoinedThisMonthTask;
            var membersJoinedPreviousMonth = await membersJoinedPreviousMonthTask;

            var dashboardDto = new GroupDashboardDTO
            {
                MemberStats = new MetricCardData
                {
                    Value = currentMemberCount.ToString("N0"),
                    Label = "Tổng thành viên",
                    PercentageChange = CalculatePercentageChange(membersJoinedThisMonth, membersJoinedPreviousMonth)
                },
                WeeklyPostStats = new MetricCardData
                {
                    Value = (await postsThisWeekTask).ToString("N0"),
                    Label = "Bài viết trong 7 ngày"
                },
                InteractionStats = new MetricCardData
                {
                    Value = (await interactionsThisWeekTask).ToString("N0"),
                    Label = "Lượt thích & bình luận (7 ngày)"
                },
                PendingReportsStats = new MetricCardData
                {
                    Value = (await pendingReportsCountTask).ToString(),
                    Label = "Cần xem xét ngay"
                },
                ActiveMemberStats = new MetricCardData
                {
                    Value = (await activeMemberCountTask).ToString("N0"),
                    Label = "Hoạt động trong 7 ngày"
                },
                ReportResponseTimeStats = new MetricCardData
                {
                    Value = "N/A",
                    Label = "Trung bình xử lý báo cáo"
                }
            };

            return ApiResponse<GroupDashboardDTO>.Ok(dashboardDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu dashboard cho nhóm {GroupId}", groupId);
            return ApiResponse<GroupDashboardDTO>.Fail("ServerError", "Đã có lỗi hệ thống xảy ra.", 500);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Kiểm tra người dùng có phải là Admin hoặc Mod của nhóm không.
    /// </summary>
    private async Task<bool> IsUserAdminOrMod(Guid groupId, Guid userId)
    {
        var membership = await _unitOfWork.GroupMembers.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);
        return membership != null && membership.Role > EnumGroupRole.Member;
    }

    /// <summary>
    /// Đếm tổng số lượt thích và bình luận trong một khoảng thời gian.
    /// </summary>
    private async Task<int> GetInteractionCountAsync(Guid groupId, DateTime since)
    {
        var likesTask = _unitOfWork.PostLikes.GetQueryable().CountAsync(l => l.Post.GroupID == groupId && l.CreatedAt >= since);
        var commentsTask = _unitOfWork.PostComments.GetQueryable().CountAsync(c => c.Post.GroupID == groupId && c.CreatedAt >= since);

        await Task.WhenAll(likesTask, commentsTask);

        return await likesTask + await commentsTask;
    }

    /// <summary>
    /// Đếm số lượng thành viên duy nhất đã đăng bài hoặc bình luận.
    /// </summary>
    private Task<int> GetActiveMemberCountAsync(Guid groupId, DateTime since)
    {
        var postAuthors = _unitOfWork.Posts.GetQueryable()
            .Where(p => p.GroupID == groupId && p.CreatedAt >= since)
            .Select(p => p.AuthorUserID);

        var commentAuthors = _unitOfWork.PostComments.GetQueryable()
            .Where(c => c.Post.GroupID == groupId && c.CreatedAt >= since)
            .Select(c => c.UserID);

        return postAuthors.Union(commentAuthors).Distinct().CountAsync();
    }

    /// <summary>
    /// Tính toán sự thay đổi phần trăm giữa hai giá trị.
    /// </summary>
    private decimal? CalculatePercentageChange(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            // Nếu giá trị trước đó là 0, bất kỳ sự gia tăng nào cũng là 100%
            return (current > 0) ? 100.0m : 0.0m;
        }
        return Math.Round(((current - previous) / previous) * 100, 2);
    }

    #endregion

    public async Task<ApiResponse<ModerationOverviewDTO>> GetModerationOverviewAsync(Guid groupId)
    {
        // Bước kiểm tra quyền có thể được tái sử dụng hoặc viết lại
        if (!await IsUserAdminOrMod(groupId))
            return ApiResponse<ModerationOverviewDTO>.Fail("Forbidden", "Bạn không có quyền truy cập.", 403);

        try
        {
            var now = DateTime.UtcNow;
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek); // Giả sử tuần bắt đầu từ Chủ Nhật
            var thirtyDaysAgo = now.AddDays(-30);

            // Chạy các query song song
            var pendingReportsCountTask = _unitOfWork.ContentReports.GetQueryable().CountAsync(r => r.GroupID == groupId && r.Status == EnumContentReportStatus.Pending);

            var resolvedThisWeekCountTask = _unitOfWork.ContentReports.GetQueryable().CountAsync(r =>
                r.GroupID == groupId &&
                r.Status != EnumContentReportStatus.Pending &&
                r.UpdatedAt.HasValue && r.UpdatedAt.Value >= startOfWeek);

            // Query để tính thời gian phản hồi trung bình
            var resolvedReportsForTimingTask = _unitOfWork.ContentReports.GetQueryable()
                .Where(r => r.GroupID == groupId && r.Status != EnumContentReportStatus.Pending && r.UpdatedAt.HasValue && r.UpdatedAt >= thirtyDaysAgo)
                .Select(r => new { r.CreatedAt, r.UpdatedAt })
                .ToListAsync();

            await Task.WhenAll(pendingReportsCountTask, resolvedThisWeekCountTask, resolvedReportsForTimingTask);

            // Tính toán thời gian phản hồi
            string avgResponseTime = "N/A";
            var resolvedReportsForTiming = await resolvedReportsForTimingTask;
            if (resolvedReportsForTiming.Any())
            {
                var totalHours = resolvedReportsForTiming.Average(r => (r.UpdatedAt!.Value - r.CreatedAt).TotalHours);
                avgResponseTime = $"{Math.Round(totalHours, 1)}h";
            }

            var overviewDto = new ModerationOverviewDTO
            {
                PendingReportsCount = await pendingReportsCountTask,
                ResolvedReportsThisWeekCount = await resolvedThisWeekCountTask,
                AverageResponseTime = avgResponseTime
            };

            return ApiResponse<ModerationOverviewDTO>.Ok(overviewDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dữ liệu tổng quan kiểm duyệt cho nhóm {GroupId}", groupId);
            return ApiResponse<ModerationOverviewDTO>.Fail("ServerError", "Đã có lỗi hệ thống xảy ra.", 500);
        }
    }

    // Helper để kiểm tra quyền, tránh lặp code
    private async Task<bool> IsUserAdminOrMod(Guid groupId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId)) return false;
        var membership = await _unitOfWork.GroupMembers.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == currentUserId);
        return membership != null && membership.Role != EnumGroupRole.Member;
    }
}
