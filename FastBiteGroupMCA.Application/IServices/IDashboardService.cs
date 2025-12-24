using FastBiteGroupMCA.Application.DTOs.Group.Admin;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices;

public interface IDashboardService
{
    /// <summary>
    /// Lấy dữ liệu thống kê tổng quan cho dashboard quản lý nhóm.
    /// </summary>
    /// <param name="groupId">ID của nhóm cần lấy thống kê.</param>
    /// <returns>Dữ liệu thống kê hoặc lỗi nếu không có quyền.</returns>
    Task<ApiResponse<GroupDashboardDTO>> GetGroupDashboardStatsAsync(Guid groupId);
    Task<ApiResponse<ModerationOverviewDTO>> GetModerationOverviewAsync(Guid groupId);
}
