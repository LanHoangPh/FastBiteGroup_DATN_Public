using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.DTOs.SharedFile;
using FastBiteGroupMCA.Application.Response.IFileStorage;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Infastructure.Hubs;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace FastBiteGroupMCA.Infastructure.Services;

public class FileService : IFileService
{
    private readonly IUnitOfWork _sqlUnitOfWork;
    private readonly IMessagesRepository _messagesRepository;
    private readonly StorageStrategy _storageStrategy;
    private readonly ISettingsService _settingsService;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<FileService> _logger;

    public FileService(IUnitOfWork sqlUnitOfWork, IMessagesRepository messagesRepository, StorageStrategy storageStrategy, ICurrentUser currentUser, IMapper mapper, IHubContext<ChatHub> hubContext, ILogger<FileService> logger, ISettingsService settingsService)
    {
        _sqlUnitOfWork = sqlUnitOfWork;
        _messagesRepository = messagesRepository;
        _storageStrategy = storageStrategy;
        _currentUser = currentUser;
        _mapper = mapper;
        _hubContext = hubContext;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<ApiResponse<UploadedFileDTO>> UploadAttachmentAsync(int conversationId, IFormFile file)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<UploadedFileDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        // 1. Kiểm tra quyền: người dùng phải là thành viên cuộc hội thoại
        if (!await _sqlUnitOfWork.ConversationParticipants.GetQueryable()
            .AnyAsync(p => p.ConversationID == conversationId && p.UserID == userId))
            return ApiResponse<UploadedFileDTO>.Fail("Forbidden", "Bạn không có quyền tải file lên cuộc hội thoại này.");

        // 2. Tải file lên dịch vụ lưu trữ (Azure, Cloudinary...)
        var storageService = _storageStrategy.GetStorageService(file.ContentType);
        var uploadResult = await storageService.UploadAsync(file, "attachments");
        if (!uploadResult.Success)
            return ApiResponse<UploadedFileDTO>.Fail("UploadFailed", "Tải file lên thất bại.");

        // 3. Tạo bản ghi metadata trong SQL
        var newFile = new SharedFiles
        {
            FileName = file.FileName,
            StorageUrl = uploadResult.Url,
            FileSize = file.Length,
            FileType = file.ContentType,
            UploadedByUserID = userId,
            UploadedAt = DateTime.UtcNow,
        };
        await _sqlUnitOfWork.SharedFiles.AddAsync(newFile);
        await _sqlUnitOfWork.SaveChangesAsync();

        // 4. Trả về thông tin file đã tải lên
        var dto = _mapper.Map<UploadedFileDTO>(newFile);
        return ApiResponse<UploadedFileDTO>.Ok(dto, "Tải file lên thành công.");
    }
    private async Task CleanupUploadedFiles(List<FileUploadResult> uploadedFiles)
    {
        if (!uploadedFiles.Any()) return;

        _logger.LogWarning("Starting cleanup for {FileCount} orphaned files.", uploadedFiles.Count);

        foreach (var uploadedFile in uploadedFiles)
        {
            try
            {
                var storageService = _storageStrategy.GetStorageService(uploadedFile.ContentType);
                var identifierToDelete = uploadedFile.PublicId ?? uploadedFile.Url;
                await storageService.DeleteAsync(identifierToDelete);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Failed to cleanup file: {Url}", uploadedFile.Url);
            }
        }
    }

    private string GetMessagePreview(Messages message)
    {
        return message.MessageType switch
        {
            EnumMessageType.Image => "Đã gửi một ảnh",
            EnumMessageType.Video => "Đã gửi một video",
            EnumMessageType.File => "Đã gửi một tệp",
            EnumMessageType.Audio => "Đã gửi một tin nhắn thoại",
            _ => message.Attachments.FirstOrDefault()?.FileName ?? "Tệp đính kèm"
        };
    }

    private EnumMessageType DetermineMessageType(string contentType)
    {
        contentType = contentType.ToLower();
        if (contentType.StartsWith("image/")) return EnumMessageType.Image;
        if (contentType.StartsWith("video/")) return EnumMessageType.Video;
        if (contentType.StartsWith("audio/")) return EnumMessageType.Audio;
        return EnumMessageType.File;
    }

    public async Task<ApiResponse<StagingUploadResponseDto>> UploadStagingAvatarAsync(IFormFile avatarFile, string category)
    {
        var maxFileSizeMb = _settingsService.Get<int>(SettingKeys.MaxFileSizeMb, 10);
        var maxFileSizeBytes = maxFileSizeMb * 1024 * 1024;

        if (avatarFile == null || avatarFile.Length == 0)
            return ApiResponse<StagingUploadResponseDto>.Fail("FILE_EMPTY", "Không có file nào được chọn.");

        if (avatarFile.Length > maxFileSizeBytes)
            return ApiResponse<StagingUploadResponseDto>.Fail("FILE_TOO_LARGE", $"Kích thước file không được vượt quá {maxFileSizeMb}MB");

        // Quan trọng: Chỉ chấp nhận file ảnh
        if (!avatarFile.ContentType.ToLower().StartsWith("image/"))
            return ApiResponse<StagingUploadResponseDto>.Fail("INVALID_FILE_TYPE", "Chỉ chấp nhận file định dạng ảnh.");

        try
        {
            var storageService = _storageStrategy.GetStorageService(avatarFile.ContentType);

            var uploadResult = await storageService.UploadAsync(avatarFile, category);

            if (!uploadResult.Success)
                return ApiResponse<StagingUploadResponseDto>.Fail("UPLOAD_FAILED", "Tải ảnh lên thất bại.");

            var responseDto = new StagingUploadResponseDto { FileUrl = uploadResult.Url };
            return ApiResponse<StagingUploadResponseDto>.Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during staging group avatar upload.");
            throw; 
        }
    }

    public async Task<ApiResponse<FileUploadResult>> UploadAvatarAsync(IFormFile file, string subfolder, string context)
    {
        if (file == null || file.Length == 0)
            return ApiResponse<FileUploadResult>.Fail("Validation", "File là bắt buộc.");

        // Lấy cấu hình động từ CSDL
        var maxAvatarSizeMb = _settingsService.Get<int>(SettingKeys.MaxAvatarSizeMb, 5); // Mặc định 5MB
        var maxAvatarSizeBytes = maxAvatarSizeMb * 1024 * 1024;
        var allowedAvatarTypesCsv = _settingsService.Get<string>(SettingKeys.AllowedAvatarTypes, "jpg,jpeg,png,gif");
        var allowedAvatarExtensions = allowedAvatarTypesCsv.Split(',').Select(t => $".{t.Trim().ToLower()}").ToList();

        // Validate bằng cấu hình động
        if (file.Length > maxAvatarSizeBytes)
            return ApiResponse<FileUploadResult>.Fail("Validation", $"Ảnh đại diện không được lớn hơn {maxAvatarSizeMb}MB.");

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!file.ContentType.StartsWith("image/") || !allowedAvatarExtensions.Contains(fileExtension))
            return ApiResponse<FileUploadResult>.Fail("Validation", $"Chỉ chấp nhận các định dạng ảnh: {allowedAvatarTypesCsv}.");

        // Logic upload giữ nguyên
        var storageService = _storageStrategy.GetStorageService(file.ContentType);
        var uploadResult = await storageService.UploadAsync(file, subfolder);

        if (!uploadResult.Success)
            return ApiResponse<FileUploadResult>.Fail("UploadFailed", "Không thể tải ảnh đại diện lên.");

        return ApiResponse<FileUploadResult>.Ok(uploadResult);
    }
    // --- PHƯƠNG THỨC MỚI CHO UPLOAD FILE CHUNG ---
    public async Task<ApiResponse<FileUploadResponseDto>> UploadGeneralFileAsync(IFormFile file, string context)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<FileUploadResponseDto>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        // 1. Validate file dựa trên cấu hình động
        var maxFileSizeMb = _settingsService.Get<int>(SettingKeys.MaxFileSizeMb, 10);
        var maxFileSizeBytes = maxFileSizeMb * 1024 * 1024;
        if (file == null || file.Length == 0)
            return ApiResponse<FileUploadResponseDto>.Fail("Validation", "File là bắt buộc.");
        if (file.Length > maxFileSizeBytes)
            return ApiResponse<FileUploadResponseDto>.Fail("Validation", $"Kích thước file không được lớn hơn {maxFileSizeMb}MB.");

        // (Thêm validation loại file nếu cần)

        // 2. Upload file lên cloud storage
        var storageService = _storageStrategy.GetStorageService(file.ContentType);
        var uploadResult = await storageService.UploadAsync(file, context); // context có thể dùng làm subfolder
        if (!uploadResult.Success)
            return ApiResponse<FileUploadResponseDto>.Fail("UPLOAD_FAILED", "Không thể tải file lên.");

        // 3. Lưu thông tin file vào CSDL SQL
        var newSharedFile = new SharedFiles
        {
            FileName = file.FileName,
            StorageUrl = uploadResult.Url,
            FileSize = file.Length,
            FileType = file.ContentType,
            UploadedByUserID = userId,
            UploadedAt = DateTime.UtcNow,
            FileContext = context // Lưu lại ngữ cảnh upload
        };
        await _sqlUnitOfWork.SharedFiles.AddAsync(newSharedFile);
        await _sqlUnitOfWork.SaveChangesAsync(); // Lưu để lấy FileID

        // 4. Trả về DTO chứa thông tin cần thiết cho frontend
        var responseDto = new FileUploadResponseDto
        {
            FileId = newSharedFile.FileID,
            FileName = newSharedFile.FileName,
            StorageUrl = newSharedFile.StorageUrl,
            FileType = newSharedFile.FileType ?? "",
            FileSize = newSharedFile.FileSize
        };

        return ApiResponse<FileUploadResponseDto>.Ok(responseDto);
    }

    public async Task<ApiResponse<List<FileUploadResponseDto>>> UploadMultipleFilesAsync(List<IFormFile> files, string context)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<List<FileUploadResponseDto>>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var successfulUploads = new List<FileUploadResponseDto>();
        var createdSharedFiles = new List<SharedFiles>();

        await using var transaction = await _sqlUnitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var file in files)
            {
                var storageService = _storageStrategy.GetStorageService(file.ContentType);
                var uploadResult = await storageService.UploadAsync(file, context);
                if (!uploadResult.Success)
                {
                    throw new InvalidOperationException($"Upload file '{file.FileName}' thất bại.");
                }

                // Tạo entity nhưng chưa save
                var newSharedFile = new SharedFiles
                {
                    FileName = file.FileName,
                    StorageUrl = uploadResult.Url,
                    FileSize = file.Length,
                    FileType = file.ContentType,
                    UploadedByUserID = userId,
                    UploadedAt = DateTime.UtcNow,
                    FileContext = context
                };
                createdSharedFiles.Add(newSharedFile);
            }

            // Thêm tất cả các bản ghi file vào DbContext
            await _sqlUnitOfWork.SharedFiles.AddRangeAsync(createdSharedFiles);
            // Lưu tất cả vào CSDL trong một lần gọi
            await _sqlUnitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            // Map kết quả sang DTO để trả về
            successfulUploads = _mapper.Map<List<FileUploadResponseDto>>(createdSharedFiles);
            return ApiResponse<List<FileUploadResponseDto>>.Ok(successfulUploads);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error uoload muilti file");
            return ApiResponse<List<FileUploadResponseDto>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra trong quá trình xử lý file.");
        }
    }
}
