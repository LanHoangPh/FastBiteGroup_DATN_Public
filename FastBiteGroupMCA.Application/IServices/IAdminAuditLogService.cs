using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.IServices;

public interface IAdminAuditLogService
{
    /// <summary>
    /// Ghi lại một hành động quản trị vào nhật ký.
    /// </summary>
    /// <param name="actionType">Loại hành động (ví dụ: "USER_DEACTIVATED").</param>
    /// <param name="targetEntityType">Loại đối tượng bị tác động (ví dụ: "User", "Group").</param>
    /// <param name="targetEntityId">ID của đối tượng bị tác động.</param>
    /// <param name="details">Thông tin chi tiết thêm (ví dụ: lý do).</param>
    Task LogAdminActionAsync(Guid adminId, string adminFullName, EnumAdminActionType actionType, EnumTargetEntityType targetEntityType, string targetEntityId, string? details = null, Guid? batchId = null);
    Task<ApiResponse<PagedResult<AdminAuditLogDto>>> GetLogsAsync(GetAdminAuditLogsParams request);
    /// <summary>
    /// Lấy chi tiết một bản ghi nhật ký theo ID.
    /// </summary>
    Task<ApiResponse<AdminAuditLogDto>> GetLogByIdAsync(long id);
}
