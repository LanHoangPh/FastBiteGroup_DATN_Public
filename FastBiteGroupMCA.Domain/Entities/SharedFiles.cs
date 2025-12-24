using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Domain.Entities;

public class SharedFiles : ISoftDelete
{
    public int FileID { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public Guid UploadedByUserID { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
    // --- BỔ SUNG CỘT MỚI ---
    /// <summary>
    /// Ngữ cảnh sử dụng của file, ví dụ: "PostAttachment", "UserAvatar", "GroupAvatar".
    /// </summary>
    [StringLength(50)]
    public string FileContext { get; set; } = string.Empty;

    public virtual AppUser? UploadedByUser { get; set; }
}
