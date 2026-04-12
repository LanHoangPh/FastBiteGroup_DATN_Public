using FastBiteGroupMCA.Infastructure.Messaging;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace FastBiteGroupMCA.Infastructure.Services;

public class SettingsService : ISettingsService
{
    private ConcurrentDictionary<string, string> _settingsCache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SettingsService> _logger;
    private readonly IPubSubService _pubSubService;
    public SettingsService(IServiceProvider serviceProvider, ILogger<SettingsService> logger, IPubSubService pubSubService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _pubSubService = pubSubService;
        _settingsCache = new ConcurrentDictionary<string, string>();
        ReloadCache();
        _pubSubService.SubscribeToSettingsUpdates(() => ReloadCache());
    }
    private void ReloadCache()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            _logger.LogInformation("Reloading all global settings from database into cache.");
            var settingsFromDb = unitOfWork.GlobalSettings.GetQueryable()
                                     .ToDictionary(s => s.SettingKey, s => s.SettingValue);
            _settingsCache = new ConcurrentDictionary<string, string>(settingsFromDb);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload settings cache from database.");
        }
    }

    public T Get<T>(SettingKeys key, T defaultValue)
    {
        if (_settingsCache.TryGetValue(key.ToString(), out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception)
            {
                return defaultValue; 
            }
        }
        return defaultValue;
    }

    public Dictionary<string, string> GetAllSettings()
    {
        return new Dictionary<string, string>(_settingsCache);
    }

    public async Task UpdateSettingsAsync(Dictionary<string, string> settingsToUpdate)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAdminAuditLogService>();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        if (!Guid.TryParse(currentUser.Id, out var adminId) || string.IsNullOrEmpty(currentUser.FullName))
        {
            _logger.LogWarning("Không thể ghi log cập nhật setting vì không có thông tin admin.");
        }

        var changesToLog = new List<(string Key, string OldValue, string NewValue)>();

        foreach (var setting in settingsToUpdate)
        {
            var existingSetting = await unitOfWork.GlobalSettings.GetQueryable()
                                        .FirstOrDefaultAsync(s => s.SettingKey == setting.Key);

            var oldValue = existingSetting?.SettingValue ?? "[Không tồn tại]";

            if (oldValue != setting.Value)
            {
                changesToLog.Add((setting.Key, oldValue, setting.Value));
            }

            if (existingSetting != null)
            {
                existingSetting.SettingValue = setting.Value;
                unitOfWork.GlobalSettings.Update(existingSetting);
            }
            else
            {
                await unitOfWork.GlobalSettings.AddAsync(new GlobalSettings { SettingKey = setting.Key, SettingValue = setting.Value });
            }
            _settingsCache[setting.Key] = setting.Value;
        }

        if (changesToLog.Any())
        {
            await unitOfWork.SaveChangesAsync();
            _logger.LogInformation("{Count} global settings updated in database and cache by Admin {AdminId}.", changesToLog.Count, adminId);
            foreach (var change in changesToLog)
            {
                backgroundJobClient.Enqueue(() => auditLogService.LogAdminActionAsync(
                    adminId,
                    currentUser.FullName!,
                    EnumAdminActionType.SettingsUpdated,
                    EnumTargetEntityType.Setting,      
                    change.Key,
                    $"Admin đã cập nhật cài đặt '{change.Key}' từ '{change.OldValue}' thành '{change.NewValue}'.",
                    null 
                ));
            }
            await _pubSubService.PublishSettingsUpdateAsync();
        }
    }
}
