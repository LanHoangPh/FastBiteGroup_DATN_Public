namespace FastBiteGroupMCA.Domain.Entities;

public class Posts : IDateTracking, ISoftDelete
{
    public int PostID { get; set; }
    public Guid GroupID { get; set; }
    public Guid AuthorUserID { get; set; }
    public string? Title { get; set; }
    public string ContentJson { get; set; } = string.Empty; // sẽ chứa Json để lưu nội dung bài viết
    public string ContentHtml { get; set; } = string.Empty; // sẽ chứa HTML để hiển thị nội dung bài viết
    public bool IsPinned { get; set; } // để đánh dấu bài viết được ghim lên đầu nhóm
    public bool IsDeleted { get; set; }
    public EnumPostStatus Status { get; set; } = EnumPostStatus.PendingReview;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public virtual Group? Group { get; set; }
    public virtual AppUser? Author { get; set; }
    public virtual ICollection<PostComments> Comments { get; set; } = new HashSet<PostComments>();
    public virtual ICollection<PostLikes> Likes { get; set; } = new HashSet<PostLikes>();
    public virtual ICollection<PostAttachment> Attachments { get; set; } = new HashSet<PostAttachment>();
}
