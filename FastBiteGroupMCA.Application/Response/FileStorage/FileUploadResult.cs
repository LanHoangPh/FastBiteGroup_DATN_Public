namespace FastBiteGroupMCA.Application.Response.IFileStorage
{
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? PublicId { get; set; } // Dành riêng cho Cloudinary để xóa
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
