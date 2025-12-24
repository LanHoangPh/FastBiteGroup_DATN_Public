namespace FastBiteGroupMCA.Domain.Entities;

public class PostComments : ISoftDelete
{
    public int CommentID { get; set; }
    public int PostID { get; set; }
    public Guid UserID { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? ParentCommentID { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public virtual Posts? Post { get; set; }
    public virtual AppUser? User { get; set; }
    public virtual PostComments? ParentComment { get; set; }
    public virtual ICollection<PostComments> Replies { get; set; } = new HashSet<PostComments>();
}
