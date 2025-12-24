namespace FastBiteGroupMCA.Domain.Entities;

public class PostLikes
{
    public long LikeID { get; set; }
    public int PostID { get; set; }
    public Guid UserID { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Posts? Post { get; set; }
    public virtual AppUser? User { get; set; }
}
