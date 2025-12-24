using FastBiteGroupMCA.Application.DTOs.SharedFile;
using FastBiteGroupMCA.Application.DTOs.User;

namespace FastBiteGroupMCA.Application.DTOs.Post;

public class PostSummaryDTO
{
    public int PostId { get; set; }
    public string? Title { get; set; }
    public PostAuthorDTO Author { get; set; } = null!;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public List<PostAttachmentDTO> Attachments { get; set; } = new();

    // --- BỔ SUNG CÁC CỜ QUYỀN HẠN ---
    /// <summary>
    /// Người dùng hiện tại có quyền sửa bài viết này không? (Là tác giả)
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Người dùng hiện tại có quyền xóa bài viết này không? (Là tác giả hoặc Admin/Mod)
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Người dùng hiện tại có quyền ghim bài viết này không? (Là Admin/Mod)
    /// </summary>
    public bool CanPin { get; set; }
}
