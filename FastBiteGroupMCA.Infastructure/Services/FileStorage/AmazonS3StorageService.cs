using Amazon.S3.Model;
using Amazon.S3;
using Amazon;
using FastBiteGroupMCA.Application.IServices.FileStorage;
using FastBiteGroupMCA.Application.Response.IFileStorage;
using FastBiteGroupMCA.Infastructure.DependencyInjection.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FastBiteGroupMCA.Infastructure.Services.FileStorage;

public class AmazonS3StorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AmazonS3Options _options;
    private readonly ILogger<AmazonS3StorageService> _logger;

    public AmazonS3StorageService(IOptions<AmazonS3Options> options, ILogger<AmazonS3StorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _s3Client = new AmazonS3Client(
            _options.AccessKeyId,
            _options.SecretAccessKey,
            RegionEndpoint.GetBySystemName(_options.Region)
        );
    }

    /// <summary>
    /// Quyết định xem service này sẽ xử lý loại file nào.
    /// Ví dụ: chỉ xử lý file PDF và tài liệu Word.
    /// </summary>
    public bool CanHandle(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var type = contentType.ToLower();
        var allowedTypes = new HashSet<string>
    {
        // Audio
        "audio/mpeg", "audio/wav", "audio/ogg", "audio/flac",
        
        // Video  
        "video/mp4", "video/avi", "video/mov", "video/wmv", "video/mkv",
        
        // Images
        "image/jpeg", "image/png", "image/gif", "image/bmp", "image/svg+xml", "image/webp",
        
        // Documents
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // DOCX
        "application/msword", // DOC
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // XLSX
        "application/vnd.ms-excel", // XLS
        "application/vnd.openxmlformats-officedocument.presentationml.presentation", // PPTX
        "application/vnd.ms-powerpoint", // PPT
        "text/plain", // TXT
        "application/rtf",
        
        // Archives
        "application/zip", "application/x-rar-compressed", "application/x-7z-compressed",
        "application/x-tar", "application/gzip",
        
        // Other
        "application/json", "application/xml", "text/csv", "text/markdown",
        "application/octet-stream"
    };

        // Check prefix types
        if (type.StartsWith("audio/") || type.StartsWith("video/") || type.StartsWith("image/"))
            return true;

        return allowedTypes.Contains(type);
    }

    public async Task<FileUploadResult> UploadAsync(IFormFile file, string folder)
    {
        var key = $"{folder}/{Guid.NewGuid()}_{file.FileName}";
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType,
            };

            await _s3Client.PutObjectAsync(request);

            var preSignedUrl = GeneratePreSignedUrlWithDownloadHeader(key, file.FileName);

            return new FileUploadResult
            {
                Success = true,
                Url = preSignedUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWS S3 upload failed for file {FileName}", file.FileName);
            return new FileUploadResult { Success = false };
        }
    }

    public async Task<bool> DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return false;

        try
        {
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/');

            var request = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            _logger.LogInformation("File deleted from S3: {FileUrl}", fileUrl);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Attempted to delete a non-existent file from S3: {FileUrl}", fileUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from S3: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<FileUploadResult> UploadAsync(byte[] fileBytes, string fileName, string contentType, string folder)
    {
        var key = $"{folder}/{Guid.NewGuid()}_{fileName}";
        try
        {
            await using var stream = new MemoryStream(fileBytes);

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
            };

            await _s3Client.PutObjectAsync(request);

            var preSignedUrl = GeneratePreSignedUrlWithDownloadHeader(key, fileName);


            return new FileUploadResult
            {
                Success = true,
                Url = preSignedUrl,
                FileName = fileName,
                FileSize = fileBytes.Length,
                ContentType = contentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWS S3 upload failed for file {FileName} from byte array", fileName);
            return new FileUploadResult { Success = false };
        }
    }
    private string GeneratePreSignedUrlWithDownloadHeader(string key, string fileName)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddDays(7), 

            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = $"attachment; filename=\"{fileName}\""
            }
        };

        string url = _s3Client.GetPreSignedURL(request);
        return url;
    }
}
