using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.API.Midleware
{
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISettingsService _settingsService;

        private readonly string[] _whitelistedPaths =
        {
            "/login",             
            "/api/v1/auth/login",    
            "/api/v1/auth/token",   
        };

        public MaintenanceMiddleware(RequestDelegate next, ISettingsService settingsService)
        {
            _next = next;
            _settingsService = settingsService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var maintenanceModeEnabled = _settingsService.Get<bool>(SettingKeys.MaintenanceMode, false);

            if (maintenanceModeEnabled)
            {
                var path = context.Request.Path.Value ?? string.Empty;

                var isAdmin = context.User.IsInRole("Admin");

                var isWhitelisted = _whitelistedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));


                if (!isAdmin && !isWhitelisted)
                {
                    context.Response.StatusCode = 503;
                    await context.Response.WriteAsync("Hệ thống đang bảo trì. Vui lòng quay lại sau.");
                    return; // Dừng request tại đây
                }
            }
            await _next(context);
        }
    }
}
