using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.ContentReport;

public class GetPendingReportsQuery : PaginationParams
{
    /// <summary>
    /// (Tùy chọn) Lọc báo cáo theo loại nội dung (Post hoặc Comment).
    /// </summary>
    public EnumReportedContentType? ContentType { get; set; }

    /// <summary>
    /// (Tùy chọn) Lọc báo cáo được gửi bởi một người dùng cụ thể.
    /// </summary>
    public Guid? ReporterId { get; set; }

    /// <summary>
    /// (Tùy chọn) Lọc các báo cáo nhắm vào nội dung của một tác giả cụ thể.
    /// </summary>
    public Guid? AuthorId { get; set; }

    /// <summary>
    /// (Tùy chọn) Sắp xếp kết quả. Mặc định là "newest".
    /// Các giá trị có thể là: "newest", "oldest".
    /// </summary>
    public string SortBy { get; set; } = "newest";
}
