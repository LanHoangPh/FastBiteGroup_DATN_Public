using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/admin/dashboard")]
[ApiController]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Admin-v1")] 
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// [Admin] Lấy dữ liệu tổng quan cho trang Dashboard.
    /// </summary>
    [HttpGet("summary-ad")]
    public async Task<IActionResult> GetSummaryAD()
    {
        var result = await _dashboardService.GetDashboardSummaryADAsync();
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Lấy dữ liệu biểu đồ chi tiết cho trang Analytics.
    /// </summary>
    /// <param name="request">Chứa tham số về khoảng thời gian (TimeRange).</param>
    [HttpGet("charts")]
    public async Task<IActionResult> GetChartData([FromQuery] GetAnalyticsDataRequest request)
    {
        var result = await _dashboardService.GetAnalyticsDataAsync(request);
        return Ok(result);
    }
}
