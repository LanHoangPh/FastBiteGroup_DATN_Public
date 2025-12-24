using FastBiteGroupMCA.Application.DTOs.OneSignal;
using FastBiteGroupMCA.Infastructure.DependencyInjection.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FastBiteGroupMCA.Infastructure.Services;

public class OneSignalService : IOneSignalService
{
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OneSignalSettings _settings;
    private readonly ILogger<OneSignalService> _logger;

    public OneSignalService(HttpClient httpClient, IUnitOfWork unitOfWork, IOptions<OneSignalSettings> settings, ILogger<OneSignalService> logger)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendNotificationToUserAsync(string message, Guid userId, string? webUrl = null)
    {
        var user = await _unitOfWork.Users.GetQueryable()
            .Where(u => u.Id == userId)
            .Select(u => u.OneSignalPlayerId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(user))
        {
            _logger.LogWarning("User {UserId} has no PlayerId. Skipping push notification.", userId);
            return;
        }
        await SendNotificationInternalAsync(message, new List<string> { user }, webUrl);
    }

    public async Task SendNotificationToMultipleUsersAsync(string message, List<Guid> userIds, string? webUrl = null)
    {
        var playerIds = await _unitOfWork.Users.GetQueryable()
            .Where(u => userIds.Contains(u.Id) && u.OneSignalPlayerId != null)
            .Select(u => u.OneSignalPlayerId!)
            .ToListAsync();
        await SendNotificationInternalAsync(message, playerIds, webUrl);
    }

    private async Task SendNotificationInternalAsync(string message, List<string> playerIds, string? webUrl = null)
    {
        if (!playerIds.Any()) return;

        // CẢI TIẾN: Sử dụng DTO đã định nghĩa
        var payload = new OneSignalPayload
        {
            AppId = _settings.AppId,
            IncludePlayerIds = playerIds,
            Contents = new Dictionary<string, string> { { "en", message } },
            Headings = new Dictionary<string, string> { { "en", "Thông báo mới" } },
            WebUrl = webUrl // Dễ dàng thêm các trường mới
        };

        // Sử dụng JsonSerializerOptions để bỏ qua các giá trị null
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        var jsonPayload = JsonSerializer.Serialize(payload, options);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("notifications", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send OneSignal notification. Status: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending OneSignal notification.");
        }
    }
}
