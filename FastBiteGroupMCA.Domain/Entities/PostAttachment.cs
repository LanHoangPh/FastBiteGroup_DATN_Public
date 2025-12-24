namespace FastBiteGroupMCA.Domain.Entities;

public class PostAttachment
{
    public int PostID { get; set; }
    public virtual Posts? Post { get; set; }

    // Khóa ngoại đến bảng SharedFiles
    public int FileID { get; set; }
    public virtual SharedFiles? SharedFile { get; set; }
}
