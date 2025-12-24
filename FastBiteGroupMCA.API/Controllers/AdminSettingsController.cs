using FastBiteGroupMCA.Application.DTOs.Admin.Setting;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/admin/settings")]
[ApiController]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[ApiExplorerSettings(GroupName = "Admin-v1")]
public class AdminSettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public AdminSettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Lấy tất cả các cài đặt toàn cục của hệ thống.
    /// </summary>
    [HttpGet]
    public IActionResult GetAllSettings()
    {
        var settings = _settingsService.GetAllSettings();
        return Ok(ApiResponse<Dictionary<string, string>>.Ok(settings));
    }

    /// <summary>
    /// Cập nhật các cài đặt toàn cục.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequestDto request)
    {
        if (request.Settings == null || !request.Settings.Any())
        {
            return BadRequest(ApiResponse<object>.Fail("NO_SETTINGS", "Không có cài đặt nào được cung cấp."));
        }

        await _settingsService.UpdateSettingsAsync(request.Settings);
        return Ok(ApiResponse<object>.Ok(null, "Cập nhật cài đặt thành công."));
    }
}
