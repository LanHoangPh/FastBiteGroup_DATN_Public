namespace FastBiteGroupMCA.Infastructure.DependencyInjection.Options;

public class AmazonS3Options
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
}
