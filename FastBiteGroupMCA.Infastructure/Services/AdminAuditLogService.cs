using FastBiteGroupMCA.Application.DTOs.Admin;

namespace FastBiteGroupMCA.Infastructure.Services;

public class AdminAuditLogService : IAdminAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AdminAuditLogService> _logger;

    public AdminAuditLogService(IUnitOfWork unitOfWork, ICurrentUser currentUser, ILogger<AdminAuditLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ApiResponse<AdminAuditLogDto>> GetLogByIdAsync(long id)
    {
        var logEntry = await _unitOfWork.AdminAuditLogs.GetQueryable()
            .Where(log => log.Id == id)
            .Select(log => new AdminAuditLogDto
            {
                Id = log.Id,
                AdminUserId = log.AdminUserId,
                AdminFullName = log.AdminFullName,
                ActionType = log.ActionType,
                TargetEntityType = log.TargetEntityType,
                TargetEntityId = log.TargetEntityId,
                Details = log.Details,
                Timestamp = log.Timestamp,
                BatchId = log.BatchId 
            })
            .FirstOrDefaultAsync();

        if (logEntry == null)
        {
            return ApiResponse<AdminAuditLogDto>.Fail("LOG_NOT_FOUND", "Không tìm thấy bản ghi nhật ký.");
        }

        return ApiResponse<AdminAuditLogDto>.Ok(logEntry);
    }

    public async Task<ApiResponse<PagedResult<AdminAuditLogDto>>> GetLogsAsync(GetAdminAuditLogsParams request)
    {
        var query = _unitOfWork.AdminAuditLogs.GetQueryable();

        // --- Áp dụng các bộ lọc động ---
        if (request.AdminId.HasValue)
            query = query.Where(log => log.AdminUserId == request.AdminId.Value);

        if (request.ActionType.HasValue)
            query = query.Where(log => log.ActionType == request.ActionType.Value);

        if (request.TargetEntityType.HasValue)
            query = query.Where(log => log.TargetEntityType == request.TargetEntityType);

        if (!string.IsNullOrEmpty(request.TargetEntityId))
            query = query.Where(log => log.TargetEntityId == request.TargetEntityId);

        if (request.StartDate.HasValue)
            query = query.Where(log => log.Timestamp >= request.StartDate.Value);

        if (request.EndDate.HasValue)
        {
            var endDateInclusive = request.EndDate.Value.Date.AddDays(1);
            query = query.Where(log => log.Timestamp < endDateInclusive);
        }

        if (request.BatchId.HasValue)
            query = query.Where(log => log.BatchId == request.BatchId.Value);

        var pagedResult = await query
            .OrderByDescending(log => log.Timestamp)
            .Select(log => new AdminAuditLogDto {
                Id = log.Id,
                AdminUserId = log.AdminUserId,
                AdminFullName = log.AdminFullName,
                ActionType = log.ActionType,
                TargetEntityType = log.TargetEntityType,
                TargetEntityId = log.TargetEntityId,
                Details = log.Details,
                Timestamp = log.Timestamp,
                BatchId = log.BatchId
            })
            .ToPagedResultAsync(request.PageNumber, request.PageSize);

        return ApiResponse<PagedResult<AdminAuditLogDto>>.Ok(pagedResult);
    }

    public async Task LogAdminActionAsync(Guid adminId, string adminFullName, EnumAdminActionType actionType, EnumTargetEntityType targetEntityType, string targetEntityId, string? details = null, Guid? batchId = null)
    {
        var logEntry = new AdminAuditLog
        {
            AdminUserId = adminId,
            AdminFullName = adminFullName,
            ActionType = actionType,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            Details = details,
            Timestamp = DateTime.UtcNow,
            BatchId = batchId 
        };

        try
        {
            await _unitOfWork.AdminAuditLogs.AddAsync(logEntry); // Giả sử có repo AdminAuditLogs
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to Admin Audit Log.");
            // Không ném lại lỗi để không làm hỏng hành động chính của người dùng
        }
    }
}
