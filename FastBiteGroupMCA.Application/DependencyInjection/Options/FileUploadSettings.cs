namespace FastBiteGroupMCA.Application.DependencyInjection.Options;

public class FileUploadSettings
{
    /// <summary>
    /// Tên của section trong file appsettings.json.
    /// Dùng const để tránh lỗi gõ sai chuỗi.
    /// </summary>
    public const string SectionName = "FileUploadSettings";

    /// <summary>
    /// Kích thước file tối đa cho phép, tính bằng Megabytes (MB).
    /// </summary>
    public int MaxFileSizeMb { get; set; } = 25; // Gán giá trị mặc định là 25MB
}
