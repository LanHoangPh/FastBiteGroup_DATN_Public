using FastBiteGroupMCA.Application.DTOs.SharedFile;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/files")] 
[ApiController]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;
    private readonly StorageStrategy _storageStrategy;

    public FilesController(IFileService fileService, IConfiguration configuration, ILogger<FilesController> logger, StorageStrategy storageStrategy)
    {
        _fileService = fileService;
        _logger = logger;
        _storageStrategy = storageStrategy;
    }

    /// <summary>
    /// Tải lên một file chung cho một mục đích cụ thể (ví dụ: đính kèm bài viết).
    /// </summary>
    /// <param name="file">File được tải lên.</param>
    /// <param name="context">Ngữ cảnh sử dụng file, ví dụ: 'PostAttachment'.</param>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<FileUploadResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string context = "Default")
    {
        if (file == null)
            return BadRequest(ApiResponse<object>.Fail("Validation", "File là bắt buộc."));

        var response = await _fileService.UploadGeneralFileAsync(file, context);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Tải lên một hoặc nhiều file.
    /// </summary>
    /// <remarks>
    /// Giới hạn: Tối đa 5 file và tổng dung lượng không quá 50MB.
    /// </remarks>
    /// <param name="files">Danh sách các file được tải lên.</param>
    /// <param name="context">Ngữ cảnh sử dụng file, ví dụ: 'PostAttachment'.</param>
    [HttpPost("upload-multiple")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<List<FileUploadResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadMultipleFiles([FromForm] List<IFormFile> files, [FromForm] string context = "Default")
    {
        if (files == null || !files.Any())
            return BadRequest(ApiResponse<object>.Fail("Validation", "Không có file nào được tải lên."));

        if (files.Count > 5)
            return BadRequest(ApiResponse<object>.Fail("Validation", "Chỉ được phép tải lên tối đa 5 file cùng lúc."));

        long maxTotalSizeMb = 50; 
        long maxTotalSizeBytes = maxTotalSizeMb * 1024 * 1024;
        if (files.Sum(f => f.Length) > maxTotalSizeBytes)
            return BadRequest(ApiResponse<object>.Fail("Validation", $"Tổng dung lượng các file không được vượt quá {maxTotalSizeMb}MB."));

        var response = await _fileService.UploadMultipleFilesAsync(files, context);

        return response.Success ? Ok(response) : BadRequest(response);
    }
    #if DEBUG
    /// <summary>
    /// [DEV-ONLY] Test upload một file duy nhất.
    /// </summary>
    [HttpPost("single-upload-test")]
    public async Task<IActionResult> UploadAttachment(int conversationId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("InvalidFile", "File không được để trống."));

        var response = await _fileService.UploadAttachmentAsync(conversationId, file);
        return response.Success ? Ok(response) : BadRequest(response);
    }
    /// <summary>
    /// [DEV-ONLY] Test hoạt động của StorageStrategy.
    /// </summary>
    [HttpPost("upload-strategy-test")]
    public async Task<IActionResult> TestUploadStrategy(IFormFile file)
    {
        _logger.LogInformation("TestUploadStrategy endpoint was hit.");

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Message = "No file was received." });
        }

        try
        {
            var contentType = file.ContentType;
            _logger.LogInformation("Input file content type: {ContentType}", contentType);
            var storageService = _storageStrategy.GetStorageService(contentType);
            var chosenServiceName = storageService.GetType().Name;

            _logger.LogInformation("StorageStrategy chose: {Service}", chosenServiceName);

            var uploadResult = await storageService.UploadAsync(file, "strategy-test-uploads");

            if (!uploadResult.Success)
            {
                return StatusCode(500, new
                {
                    Message = "Upload failed.",
                    ChosenStrategy = chosenServiceName,
                    Error = "The storage service failed to upload the file."
                });
            }

            return Ok(new
            {
                Message = "StorageStrategy test successful. File was uploaded to the cloud.",
                ChosenStrategy = chosenServiceName,
                InputFile = new
                {
                    FileName = file.FileName,
                    SizeInBytes = file.Length,
                    ContentType = file.ContentType
                },
                UploadResult = uploadResult 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during strategy test upload.");
            return StatusCode(500, new { Message = "An internal server error occurred.", Details = ex.Message });
        }
    }
    #endif
}