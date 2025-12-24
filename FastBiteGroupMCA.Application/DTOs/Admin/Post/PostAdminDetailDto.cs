using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Post;

public class PostAdminDetailDto
{
    // --- Thông tin Cốt lõi của Bài viết ---
    public int PostId { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty; // Nội dung gốc (HTML/JSON từ Tiptap)
    public string SanitizedContentHtml { get; set; } = string.Empty; // Nội dung đã được làm sạch để render an toàn
    public bool IsPinned { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // --- Thông tin Ngữ cảnh ---
    public Guid AuthorId { get; set; }
    public string AuthorFullName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;

    // --- Thông tin Tương tác ---
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public List<PostCommentAdminDto> Comments { get; set; } = new();

    // --- THÔNG TIN KIỂM DUYỆT (QUAN TRỌNG NHẤT) ---
    public List<PostReportDto> Reports { get; set; } = new();
}
/// <summary>
/// DTO chứa thông tin về một báo cáo vi phạm liên quan đến bài viết.
/// </summary>
public class PostReportDto
{
    public int ReportId { get; set; }
    public Guid ReportedByUserId { get; set; }
    public string ReportedByUserFullName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public EnumContentReportStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
}

/// <summary>
/// DTO chứa thông tin về một bình luận, dành cho Admin xem.
/// </summary>
public class PostCommentAdminDto
{
    public int CommentId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorFullName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
