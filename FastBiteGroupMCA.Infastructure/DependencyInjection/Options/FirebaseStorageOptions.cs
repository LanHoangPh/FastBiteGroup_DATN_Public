namespace FastBiteGroupMCA.Infastructure.DependencyInjection.Options;

public class FirebaseStorageOptions
{
    public const string SectionName = "FirebaseStorage";

    public string BucketName { get; set; } = string.Empty;
    public string CredentialsPath { get; set; } = string.Empty;
}
