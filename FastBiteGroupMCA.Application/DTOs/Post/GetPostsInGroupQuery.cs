using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Post;

public class GetPostsInGroupQuery : PaginationParams
{
    public string? SearchTerm { get; set; }
    public Guid? AuthorId { get; set; }
    public string SortBy { get; set; } = "newest";

    // --- BỔ SUNG BỘ LỌC MỚI ---
    /// <summary>
    /// Nếu là true, chỉ lấy các bài viết của người dùng đang đăng nhập.
    /// </summary>
    public bool MyPostsOnly { get; set; } = false;
}
