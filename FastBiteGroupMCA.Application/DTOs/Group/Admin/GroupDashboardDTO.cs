namespace FastBiteGroupMCA.Application.DTOs.Group.Admin;

public class GroupDashboardDTO
{
    // Thẻ "Tổng thành viên"
    public MetricCardData MemberStats { get; set; } = null!;

    // Thẻ "Bài viết tuần này"
    public MetricCardData WeeklyPostStats { get; set; } = null!;

    // Thẻ "Tương tác"
    public MetricCardData InteractionStats { get; set; } = null!;

    // Thẻ "Báo cáo chờ xử lý"
    public MetricCardData PendingReportsStats { get; set; } = null!;

    // Thẻ "Thành viên hoạt động"
    public MetricCardData ActiveMemberStats { get; set; } = null!;

    // Thẻ "Thời gian phản hồi"
    public MetricCardData ReportResponseTimeStats { get; set; } = null!;
}

// Class dùng chung cho các thẻ thống kê
public class MetricCardData
{
    /// <summary>
    /// Giá trị chính hiển thị trên thẻ (ví dụ: "24", "1,234", "2.4h").
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả phụ (ví dụ: "Lượt thích và bình luận", "Cần xem xét ngay").
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm thay đổi so với kỳ trước (ví dụ: 12, 8, -5).
    /// Có thể là null nếu không áp dụng.
    /// </summary>
    public decimal? PercentageChange { get; set; }
}
