using FastBiteGroupMCA.Application.IServices.FileStorage;
using FastBiteGroupMCA.Application.Response.IFileStorage;
using FastBiteGroupMCA.Infastructure.DependencyInjection.Options;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FastBiteGroupMCA.Infastructure.Services.FileStorage;

public class FirebaseStorageService : IFileStorageService
{
    private readonly FirebaseStorageOptions _options;
    private readonly ILogger<FirebaseStorageService> _logger;
    private readonly StorageClient _storageClient;
    public FirebaseStorageService(IOptions<FirebaseStorageOptions> options, ILogger<FirebaseStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.CredentialsPath) || !File.Exists(_options.CredentialsPath))
        {
            throw new FileNotFoundException("Firebase credentials file not found at path specified in configuration.", _options.CredentialsPath);
        }

        var credential = GoogleCredential.FromFile(_options.CredentialsPath);
        _storageClient = StorageClient.Create(credential);
    }

    public bool CanHandle(string contentType)
    {
        // Firebase sẽ không chủ động xử lý loại file nào cụ thể,
        // nó sẽ trở thành lựa chọn mặc định
        return false;
    }
    public async Task<bool> DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return false;

        try
        {
            var uri = new Uri(fileUrl);
            var objectName = string.Join('/', uri.AbsolutePath.Split('/').Skip(2)); 

            await _storageClient.DeleteObjectAsync(_options.BucketName, objectName);
            _logger.LogInformation("File deleted from Firebase Storage: {FileUrl}", fileUrl);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
        {
            _logger.LogWarning("Attempted to delete a non-existent file: {FileUrl}", fileUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from Firebase Storage: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<FileUploadResult> UploadAsync(IFormFile file, string folder)
    {
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var objectName = $"{folder}/{fileName}";

        try
        {
            await using var stream = file.OpenReadStream();
            await _storageClient.UploadObjectAsync(
                _options.BucketName,
                objectName,
                file.ContentType,
                stream
            );

            var signedUrl = GeneratePreSignedUrlWithDownloadHeader(objectName, file.ContentType, fileName);

            return new FileUploadResult
            {
                Success = true,
                Url = signedUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase Storage upload failed for file {FileName}", file.FileName);
            return new FileUploadResult { Success = false };
        }
    }

    public async Task<FileUploadResult> UploadAsync(byte[] fileBytes, string fileName, string contentType, string folder)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var objectName = $"{folder}/{uniqueFileName}";

        try
        {
            await using var stream = new MemoryStream(fileBytes);
            await _storageClient.UploadObjectAsync(
                _options.BucketName,
                objectName,
                contentType,
                stream
            );

            var signedUrl = GeneratePreSignedUrlWithDownloadHeader(objectName, contentType, fileName);

            return new FileUploadResult
            {
                Success = true,
                Url = signedUrl,
                FileName = fileName,
                FileSize = fileBytes.Length,
                ContentType = contentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase Storage upload failed for file {FileName} from byte array", fileName);
            return new FileUploadResult { Success = false };
        }
    }
    private string GeneratePreSignedUrlWithDownloadHeader(string objectName, string contentType, TimeSpan? expiration = null)
    {
        try
        {
            var duration = expiration ?? TimeSpan.FromMinutes(15);

        var urlSigner = _storageClient.CreateUrlSigner();

            var requestTemplate = UrlSigner.RequestTemplate
                .FromBucket(_options.BucketName)
                .WithObjectName(objectName)
                .WithHttpMethod(HttpMethod.Get)
                .WithQueryParameters(new Dictionary<string, IEnumerable<string>>
                {
                    { "response-content-type", new[] { contentType } },
                    { "response-content-disposition", new[] { $"attachment; filename=\"{Path.GetFileName(objectName)}\"" } }
                });


            var options = UrlSigner.Options.FromDuration(duration);

        string signedUrl = urlSigner.Sign(requestTemplate, options);

        _logger.LogInformation("Generated signed URL for {ObjectName}: {SignedUrl}", objectName, signedUrl);
        return signedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate signed URL for {ObjectName}", objectName);
            throw;
        }
    }
    private string GeneratePreSignedUrlWithDownloadHeader(string objectName, string contentType, string originalFileName, TimeSpan? expiration = null)
    {
        try
        {
            var duration = expiration ?? TimeSpan.FromMinutes(15);
            var urlSigner = _storageClient.CreateUrlSigner();
            var requestTemplate = UrlSigner.RequestTemplate
                .FromBucket(_options.BucketName)
                .WithObjectName(objectName)
                .WithHttpMethod(HttpMethod.Get)
                .WithQueryParameters(new Dictionary<string, IEnumerable<string>>
                {
                    { "response-content-type", new[] { contentType } },
                    // Sử dụng tên file gốc để người dùng tải về đúng tên
                    { "response-content-disposition", new[] { $"attachment; filename=\"{originalFileName}\"" } }
                });
            var options = UrlSigner.Options.FromDuration(duration);
            string signedUrl = urlSigner.Sign(requestTemplate, options);
            _logger.LogInformation("Generated signed URL for {ObjectName}: {SignedUrl}", objectName, signedUrl);
            return signedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate signed URL for {ObjectName}", objectName);
            throw; // Ném lại ngoại lệ để phương thức gọi có thể xử lý
        }
    }
}
