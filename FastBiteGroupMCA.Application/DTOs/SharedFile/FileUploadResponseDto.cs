namespace FastBiteGroupMCA.Application.DTOs.SharedFile;

public class FileUploadResponseDto
{
    /// <summary>
    /// ID của file đã được lưu trong CSDL của chúng ta. 
    /// Frontend sẽ dùng ID này để đính kèm vào bài viết.
    /// </summary>
    public int FileId { get; set; }

    /// <summary>
    /// Tên file gốc.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// URL để truy cập file (để hiển thị preview).
    /// </summary>
    public string StorageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Loại file (MIME type).
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file (bytes).
    /// </summary>
    public long FileSize { get; set; }
}
