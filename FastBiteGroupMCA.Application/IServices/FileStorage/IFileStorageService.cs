using FastBiteGroupMCA.Application.Response.IFileStorage;
using Microsoft.AspNetCore.Http;

namespace FastBiteGroupMCA.Application.IServices.FileStorage;

public interface IFileStorageService
{
    bool CanHandle(string contentType);
    Task<FileUploadResult> UploadAsync(IFormFile file, string folder);
    Task<FileUploadResult> UploadAsync(byte[] fileBytes, string fileName, string contentType, string folder);
    Task<bool> DeleteAsync(string fileUrl); 
}
