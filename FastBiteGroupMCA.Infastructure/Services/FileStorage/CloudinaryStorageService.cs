using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FastBiteGroupMCA.Application.IServices.FileStorage;
using FastBiteGroupMCA.Application.Response.IFileStorage;
using FastBiteGroupMCA.Infastructure.DependencyInjection.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FastBiteGroupMCA.Infastructure.Services.FileStorage
{
    public class CloudinaryStorageService : IFileStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _defaultFolder;

        public CloudinaryStorageService(IOptions<CloudinaryOptions> options)
        {
            var opts = options.Value;
            var account = new Account(opts.CloudName, opts.ApiKey, opts.ApiSecret);
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
            _defaultFolder = opts.Folder;
        }

        public bool CanHandle(string contentType)
        {
            var type = contentType.ToLower();
            return type.StartsWith("image/");
        }

        public async Task<bool> DeleteAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return false;

            var publicId = GetPublicIdFromUrl(fileUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                return false;
            }

            var resourceType = fileUrl.Contains("/video/") ? ResourceType.Video : ResourceType.Image;

            var deletionParams = new DeletionParams(publicId) { ResourceType = resourceType };
            var result = await _cloudinary.DestroyAsync(deletionParams);

            return result.Result.ToLower() == "ok" || result.Result.ToLower() == "not found";
        }

        // HÀM TIỆN ÍCH MỚI: Dùng để trích xuất PublicId
        private string? GetPublicIdFromUrl(string fileUrl)
        {
            try
            {
                var uri = new Uri(fileUrl);
                var segments = uri.AbsolutePath.Split('/');

                int uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex == -1 || uploadIndex + 1 >= segments.Length)
                {
                    return null;
                }

                var publicIdWithExtension = string.Join('/', segments.Skip(uploadIndex + 2));

                var publicId = Path.GetFileNameWithoutExtension(publicIdWithExtension);


                var folderPath = string.Join('/', segments.Skip(uploadIndex + 2).Take(segments.Length - (uploadIndex + 2) - 1));
                if (!string.IsNullOrEmpty(folderPath))
                {
                    publicId = $"{folderPath}/{publicId}";
                }

                return publicId;
            }
            catch
            {
                return null;
            }
        }

        public async Task<FileUploadResult> UploadAsync(IFormFile file, string folder)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = !string.IsNullOrEmpty(folder) ? folder : _defaultFolder,
                UseFilename = true,
                UniqueFilename = true, 
                Transformation = new Transformation().Quality("auto:good")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new FileUploadResult { Success = false };
            }

            return new FileUploadResult
            {
                Success = true,
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }

        public async Task<FileUploadResult> UploadAsync(byte[] fileBytes, string fileName, string contentType, string folder)
        {
            await using var stream = new MemoryStream(fileBytes);

            var uploadParams = new RawUploadParams 
            {
                File = new FileDescription(fileName, stream),
                Folder = !string.IsNullOrEmpty(folder) ? folder : _defaultFolder,
                UseFilename = true,
                UniqueFilename = true,
            };

            if (contentType.StartsWith("image/"))
            {
                uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Folder = !string.IsNullOrEmpty(folder) ? folder : _defaultFolder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Transformation = new Transformation().Quality("auto:good")
                };
            }
            else if (contentType.StartsWith("video/"))
            {
                uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Folder = !string.IsNullOrEmpty(folder) ? folder : _defaultFolder,
                    UseFilename = true,
                    UniqueFilename = true,
                };
            }

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new FileUploadResult { Success = false };
            }

            return new FileUploadResult
            {
                Success = true,
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId,
                FileName = fileName,
                FileSize = fileBytes.Length,
                ContentType = contentType
            };
        }
    }
}
