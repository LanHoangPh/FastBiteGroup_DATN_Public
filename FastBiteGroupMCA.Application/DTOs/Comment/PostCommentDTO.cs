using FastBiteGroupMCA.Application.DTOs.User;

namespace FastBiteGroupMCA.Application.DTOs.Comment;

public class PostCommentDTO
{
    public int CommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public PostAuthorDTO Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Số lượng trả lời để hiển thị nút "Xem X câu trả lời"
    public int ReplyCount { get; set; }

    // Cờ quyền hạn cho bình luận
    public bool CanEdit { get; set; } 
    public bool CanDelete { get; set; }
}
