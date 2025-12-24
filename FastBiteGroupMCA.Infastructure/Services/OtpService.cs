using FastBiteGroupMCA.Application.IServices.Auth;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace FastBiteGroupMCA.Infastructure.Services;

public class OtpService : IOtpService 
{
    private readonly IDatabase _redisDb;
    private readonly ILogger<OtpService> _logger;
    private const int OtpExpiryMinutes = 5;
    private const int MaxFailedAttempts = 5;

    public OtpService(IConnectionMultiplexer connectionMultiplexer, ILogger<OtpService> logger)
    {
        _redisDb = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task<string> GenerateOtpAsync(string key)
    {
        var otp = new Random().Next(100000, 999999).ToString();

        var otpData = new OtpData
        {
            Code = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
            FailedAttempts = 0
        };

        var expiry = TimeSpan.FromMinutes(OtpExpiryMinutes);
        var jsonValue = JsonSerializer.Serialize(otpData);

       await _redisDb.StringSetAsync(GetOtpKey(key), jsonValue, expiry);

        _logger.LogInformation("OTP generated for key: {Key}", key);

        return otp;
    }

    public async Task<bool> ValidateOtpAsync(string key, string otp)
    {
        var otpDataJson = await _redisDb.StringGetAsync(GetOtpKey(key));
        if (string.IsNullOrEmpty(otpDataJson))
        {
            _logger.LogWarning("OTP not found or expired for key: {Key}", key);
            return false;
        }

        var otpData = JsonSerializer.Deserialize<OtpData>(otpDataJson);
        if (otpData == null || otpData.ExpiresAt < DateTime.UtcNow)
        {
            await InvalidateOtpAsync(key);
            _logger.LogWarning("OTP expired for key: {Key}", key);
            return false;
        }

        if (otpData.FailedAttempts >= MaxFailedAttempts)
        {
            await InvalidateOtpAsync(key);
            _logger.LogWarning("OTP max attempts exceeded for key: {Key}", key);
            return false;
        }

        if (otpData.Code == otp)
        {
            await InvalidateOtpAsync(key);
            _logger.LogInformation("OTP validated successfully for key: {Key}", key);
            return true;
        }

        await IncrementFailedAttemptsAsync(key);
        _logger.LogWarning("Invalid OTP attempt for key: {Key}", key);
        return false;
    }

    public async Task InvalidateOtpAsync(string key)
    {
        //await _redisDb.KeyDeleteAsync(GetOtpKey(key));
        //await _redisDb.KeyDeleteAsync(GetFailedAttemptsKey(key));
        //_logger.LogInformation("OTP invalidated for key: {Key}", key);
        await _redisDb.KeyDeleteAsync(GetOtpKey(key));
        _logger.LogInformation("OTP invalidated for key: {Key}", key);
    }

    public async Task<int> GetFailedAttemptsAsync(string key)
    {
        var otpDataJson = await _redisDb.StringGetAsync(GetOtpKey(key));
        if (string.IsNullOrEmpty(otpDataJson))
            return 0;

        var otpData = JsonSerializer.Deserialize<OtpData>(otpDataJson);
        return otpData?.FailedAttempts ?? 0;
    }

    public async Task IncrementFailedAttemptsAsync(string key)
    {
        var cacheKey = GetOtpKey(key);
        var tran = _redisDb.CreateTransaction();
        tran.AddCondition(Condition.KeyExists(cacheKey));
        var getTask = tran.StringGetAsync(cacheKey);

        if (await tran.ExecuteAsync())
        {
            var existingValue = await getTask;
            if (existingValue.HasValue)
            {
                var otpData = JsonSerializer.Deserialize<OtpData>(existingValue.ToString());
                if (otpData != null)
                {
                    otpData.FailedAttempts++;
                    var remainingExpiry = otpData.ExpiresAt - DateTime.UtcNow;
                    if (remainingExpiry > TimeSpan.Zero)
                    {
                        // Thay thế 'options' bằng 'remainingExpiry' (kiểu TimeSpan)
                        await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(otpData), remainingExpiry);
                    }
                }
            }
        }
    }

    public async Task ResetFailedAttemptsAsync(string key)
    {
        var cacheKey = GetOtpKey(key);
        var tran = _redisDb.CreateTransaction();
        tran.AddCondition(Condition.KeyExists(cacheKey));
        var getTask = tran.StringGetAsync(cacheKey);

        if (await tran.ExecuteAsync())
        {
            var existingValue = await getTask;
            if (existingValue.HasValue)
            {
                var otpData = JsonSerializer.Deserialize<OtpData>(existingValue.ToString());
                if (otpData != null)
                {
                    otpData.FailedAttempts = 0; // Reset về 0
                    var remainingExpiry = otpData.ExpiresAt - DateTime.UtcNow;
                    if (remainingExpiry > TimeSpan.Zero)
                    {
                        // Thay thế 'options' bằng 'remainingExpiry' (kiểu TimeSpan)
                        await _redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(otpData), remainingExpiry);
                    }
                }
            }
        }
    }

    private static string GetOtpKey(string key) => $"otp:{key}";
    //private static string GetFailedAttemptsKey(string key) => $"otp_attempts:{key}";

    private class OtpData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int FailedAttempts { get; set; }
    }
}
