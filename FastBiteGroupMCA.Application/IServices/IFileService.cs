using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.DTOs.SharedFile;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Application.Response.IFileStorage;
using Microsoft.AspNetCore.Http;

namespace FastBiteGroupMCA.Application.IServices;

public interface IFileService
{
    Task<ApiResponse<List<FileUploadResponseDto>>> UploadMultipleFilesAsync(List<IFormFile> files, string context);
    Task<ApiResponse<UploadedFileDTO>> UploadAttachmentAsync(int conversationId, IFormFile file);
    Task<ApiResponse<FileUploadResult>> UploadAvatarAsync(IFormFile file, string subfolder, string context);
    Task<ApiResponse<FileUploadResponseDto>> UploadGeneralFileAsync(IFormFile file, string context);
    /// <summary>
    /// Tải một file ảnh lên khu vực lưu trữ tạm thời cho avatar nhóm.
    /// </summary>
    /// <param name="avatarFile">File ảnh cần tải lên.</param>
    /// <returns>URL của file đã được tải lên.</returns>
    Task<ApiResponse<StagingUploadResponseDto>> UploadStagingAvatarAsync(IFormFile avatarFile, string category);
}
