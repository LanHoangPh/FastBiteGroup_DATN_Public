using FastBiteGroupMCA.Infastructure.Redis;
using StackExchange.Redis;

namespace FastBiteGroupMCA.Infastructure.Messaging;

public class RedisPubSubService : IPubSubService
{
    private readonly ISubscriber _subscriber;
    private readonly IRedisKeyManager _keyManager;
    private readonly RedisChannel _settingsChannel;

    public RedisPubSubService(IConnectionMultiplexer redis, IRedisKeyManager keyManager)
    {
        _subscriber = redis.GetSubscriber();
        _keyManager = keyManager;
        _settingsChannel = new RedisChannel(_keyManager.SettingsUpdateChannel(), RedisChannel.PatternMode.Literal);
    }

    public async Task PublishSettingsUpdateAsync()
    {
        // Gửi một thông điệp đơn giản, chỉ cần sự kiện là đủ
        await _subscriber.PublishAsync(_settingsChannel, "reload");
    }

    public void SubscribeToSettingsUpdates(Action onMessageReceived)
    {
   _subscriber.Subscribe(_settingsChannel, (channel, message) =>
        {
            // Khi nhận được thông điệp, thực thi hành động đã được truyền vào
            onMessageReceived?.Invoke();
        });
    }
}
