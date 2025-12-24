namespace FastBiteGroupMCA.Application.DTOs.SharedFile;

public class UploadedFileDTO
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long FileSize { get; set; }
}
