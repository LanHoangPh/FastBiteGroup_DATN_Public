namespace FastBiteGroupMCA.Infastructure.DependencyInjection.Options;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";
    public string ConnectionString { get; set; } = string.Empty;

    // Tên container mặc định cho các file công khai (ví dụ: avatars)
    public string PublicContainerName { get; set; } = "public-assets";

    // Tên container mặc định cho các file riêng tư (ví dụ: attachments)
    public string PrivateContainerName { get; set; } = "private-attachments";
}
