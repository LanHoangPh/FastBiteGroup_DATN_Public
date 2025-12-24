using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices;

public interface IAdminDashboardService
{
    /// <summary>
    /// Lấy dữ liệu tổng quan cho trang dashboard của admin.
    /// </summary>
    /// <returns><see cref="DashboardSummaryDtoAD"/></returns>
    //Task<DashboardSummaryDtoAD> GetDashboardSummaryAsync();
    Task<ApiResponse<AnalyticsDto>> GetAnalyticsDataAsync(GetAnalyticsDataRequest request);

    Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryADAsync();
}
