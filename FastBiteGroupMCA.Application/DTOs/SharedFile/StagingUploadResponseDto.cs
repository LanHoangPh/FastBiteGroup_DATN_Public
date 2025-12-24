namespace FastBiteGroupMCA.Application.DTOs.SharedFile
{
    /// <summary>
    /// DTO chứa kết quả trả về sau khi tải file lên khu vực tạm thành công.
    /// </summary>
    public class StagingUploadResponseDto
    {
        /// <summary>
        /// URL công khai của file đã được tải lên.
        /// </summary>
        public string FileUrl { get; set; } = string.Empty;
    }
}
