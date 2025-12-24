using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace FastBiteGroupMCA.Infastructure.Filters
{
    public class HangfireJobFailureNotifierFilter : IServerFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HangfireJobFailureNotifierFilter> _logger;

        public HangfireJobFailureNotifierFilter(IServiceProvider serviceProvider, ILogger<HangfireJobFailureNotifierFilter> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void OnPerforming(PerformingContext context)
        {
        }

        public void OnPerformed(PerformedContext context)
        {
            string? failureMessage = null;

            // KỊCH BẢN 1: BẮT LỖI HỆ THỐNG (EXCEPTION)
            if (context.Exception != null)
            {
                var jobName = context.BackgroundJob.Job.Method.Name;
                var errorMessage = context.Exception.InnerException?.Message ?? context.Exception.Message;
                failureMessage = $"[Exception] Background Job '{jobName}' đã thất bại. Lỗi: {errorMessage}";
            }
            // KỊCH BẢN 2: BẮT LỖI NGHIỆP VỤ (BUSINESS ERROR)
            else if (context.Result is IApiResponse apiResponse && !apiResponse.Success)
            {
                var jobName = context.BackgroundJob.Job.Method.Name;
                var businessError = apiResponse.Errors?.FirstOrDefault()?.Message ?? apiResponse.Message ?? "Lỗi không xác định";
                failureMessage = $"[Business Error] Background Job '{jobName}' đã trả về lỗi. Lỗi: {businessError}";
            }

            // Nếu có bất kỳ loại lỗi nào, gửi thông báo
            if (failureMessage != null)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var adminNotificationService = scope.ServiceProvider.GetRequiredService<IAdminNotificationService>();

                        adminNotificationService.CreateAndBroadcastNotificationAsync(
                            EnumAdminNotificationType.BackgroundJobFailed,
                            failureMessage,
                            $"/admin/hangfire/jobs/failed"
                        ).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send admin notification for a failed background job.");
                }
            }
        }
    }
}
