using StackExchange.Redis;
using System.Text.Json;

namespace FastBiteGroupMCA.Infastructure.Caching;

public class RedisService : ICacheService
{
    private readonly IDatabase _redisDb;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisService> logger)
    {
        _redisDb = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var stringValue = await _redisDb.StringGetAsync(key);
        if (stringValue.IsNullOrEmpty) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(stringValue!);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cache key {Key}", key);
            return default; 
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var jsonValue = JsonSerializer.Serialize(value);
        await _redisDb.StringSetAsync(key, jsonValue, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        await _redisDb.KeyDeleteAsync(key);
    }
}
