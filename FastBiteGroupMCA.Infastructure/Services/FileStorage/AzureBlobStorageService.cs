using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using FastBiteGroupMCA.Application.IServices.FileStorage;
using FastBiteGroupMCA.Application.Response.IFileStorage;
using FastBiteGroupMCA.Infastructure.DependencyInjection.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Azure;
using Azure.Storage.Sas;

namespace FastBiteGroupMCA.Infastructure.Services.FileStorage
{
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly AzureStorageOptions _options;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IOptions<AzureStorageOptions> options, ILogger<AzureBlobStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        }

        /// <summary>
        /// Azure sẽ đóng vai trò là dịch vụ lưu trữ mặc định, "catch-all".
        /// Nó sẽ không chủ động xử lý một loại file cụ thể nào.
        /// </summary>
        public bool CanHandle(string contentType)
        {
            // Trả về false để StorageStrategy sẽ chọn nó làm phương án cuối cùng
            return false;
        }

        public async Task<FileUploadResult> UploadAsync(IFormFile file, string folder)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.PublicContainerName);

            // SỬA LỖI: Không yêu cầu public access khi tạo container
            await containerClient.CreateIfNotExistsAsync();

            var blobName = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            try
            {
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(file.OpenReadStream(), new BlobHttpHeaders { ContentType = file.ContentType });

                // TẠO SAS URL THAY VÌ URL CÔNG KHAI
                var sasUrl = GenerateSasUri(blobClient);

                return new FileUploadResult
                {
                    Success = true,
                    Url = sasUrl, // Trả về SAS URL
                    FileName = file.FileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload file lên Azure Blob Storage.");
                return new FileUploadResult { Success = false };
            }
        }

        public async Task<FileUploadResult> UploadAsync(byte[] fileBytes, string fileName, string contentType, string folder)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.PublicContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobName = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";

            try
            {
                var blobClient = containerClient.GetBlobClient(blobName);
                await using var stream = new MemoryStream(fileBytes);
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

                var sasUrl = GenerateSasUri(blobClient);

                return new FileUploadResult
                {
                    Success = true,
                    Url = sasUrl,
                    FileName = fileName,
                    FileSize = fileBytes.Length,
                    ContentType = contentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload file {FileName} từ byte array lên Azure Blob Storage.", fileName);
                return new FileUploadResult { Success = false };
            }
        }

        public async Task<bool> DeleteAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return false;

            try
            {
                var uri = new Uri(fileUrl);
                var containerName = uri.Segments[1].TrimEnd('/');
                var blobName = string.Join("", uri.Segments.Skip(2));

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                // DeleteIfExistsAsync sẽ trả về true nếu file được xóa hoặc không tồn tại
                var response = await blobClient.DeleteIfExistsAsync();

                _logger.LogInformation("Xóa file từ Azure Storage thành công (hoặc file không tồn tại): {FileUrl}", fileUrl);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Cố gắng xóa một file không tồn tại từ Azure Storage: {FileUrl}", fileUrl);
                return true; // Coi như thành công nếu file đã không tồn tại
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa file từ Azure Blob Storage: {FileUrl}", fileUrl);
                return false;
            }
        }
        private string GenerateSasUri(BlobClient blobClient)
        {
            if (!blobClient.CanGenerateSasUri)
            {
                _logger.LogWarning("Storage account key not available to generate SAS URI for blob: {BlobName}", blobClient.Name);
                return blobClient.Uri.ToString(); // Trả về URL thường nếu không có key
            }

            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b", // "b" for blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Bắt đầu có hiệu lực từ 5 phút trước
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)      // Hết hạn sau 1 giờ
            };

            // Chỉ cấp quyền đọc (Read)
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}
