using FastBiteGroupMCA.Application.DTOs.Comment;
using FastBiteGroupMCA.Application.DTOs.SharedFile;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.DTOs.Post;

public class PostDetailDTO
{
    public int PostId { get; set; }
    public string? Title { get; set; }
    public string ContentJson { get; set; } = string.Empty; // Dùng khi người dùng muốn sửa
    public string ContentHtml { get; set; } = string.Empty; // Dùng để hiển thị
    public PostAuthorDTO Author { get; set; } = null!;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLikedByCurrentUser { get; set; }

    // Cờ quyền hạn cho bài viết
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanPin { get; set; }

    public List<PostAttachmentDTO> Attachments { get; set; } = new();

    // Chỉ chứa trang đầu tiên của các bình luận cấp 1
    public PagedResult<PostCommentDTO> CommentsPage { get; set; } = null!;
}
