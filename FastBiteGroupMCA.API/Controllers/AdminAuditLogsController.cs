using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/admin/audit-logs")]
[ApiController]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[ApiExplorerSettings(GroupName = "Admin-v1")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminAuditLogService _logViewerService;
    private readonly ILogger<AdminAuditLogsController> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ICurrentUser _currentUser;
    public AdminAuditLogsController(IAdminAuditLogService logViewerService, ILogger<AdminAuditLogsController> logger, ICurrentUser currentUser, IBackgroundJobClient backgroundJobClient)
    {
        _logViewerService = logViewerService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
        _currentUser = currentUser;
    }
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs([FromQuery] GetAdminAuditLogsParams request)
    {
        _logger.LogInformation("Fetching admin audit logs with parameters: {@Request}", request);
        var result = await _logViewerService.GetLogsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Bắt đầu một tác vụ nền để xuất dữ liệu Log Kiểm toán ra file Excel.
    /// </summary>
    /// <param name="filters">Chứa các tham số lọc giống như khi lấy danh sách.</param>
    [HttpPost("export-jobs")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    public IActionResult CreateAuditLogExportJob([FromBody] GetAdminAuditLogsParams filters)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
            return Unauthorized();

        var adminFullName = _currentUser.FullName;

        _backgroundJobClient.Enqueue<IAdminExportService>(
            service => service.GenerateAuditLogsExportFileAsync(filters, adminId, adminFullName)
        );

        return Accepted(ApiResponse<object>.Ok(null, "Yêu cầu xuất file Log đã được tiếp nhận và đang được xử lý."));
    }

    /// <summary>
    /// Lấy chi tiết một bản ghi nhật ký theo ID.
    /// </summary>
    [HttpGet("{id:long}", Name = "GetAuditLogById")]
    public async Task<IActionResult> GetAuditLogById(long id)
    {
        _logger.LogInformation("Fetching admin audit log with ID: {Id}", id);
        var result = await _logViewerService.GetLogByIdAsync(id);

        return result.Success ? Ok(result) : NotFound(result);
    }
}
