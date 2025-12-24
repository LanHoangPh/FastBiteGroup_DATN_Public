using FastBiteGroupMCA.Infastructure.Redis;
using Hangfire;
using StackExchange.Redis;
using System.ComponentModel;

namespace FastBiteGroupMCA.Infastructure.Services;

public class RedisUserPresenceService : IUserPresenceService
{
    private readonly IDatabase _redisDb;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRedisKeyManager _keyManager;

    public RedisUserPresenceService(IConnectionMultiplexer connectionMultiplexer, IUnitOfWork unitOfWork, IBackgroundJobClient backgroundJobClient, IRedisKeyManager keyManager)
    {
        _redisDb = connectionMultiplexer.GetDatabase();
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _keyManager = keyManager;
    }


    public async Task UserConnectedAsync(Guid userId, string connectionId)
    {
        // THAY ĐỔI 3: Sử dụng _keyManager để lấy key
        await _redisDb.HashSetAsync(_keyManager.ConnectionsHashKey(), connectionId, userId.ToString());
        await _redisDb.SetAddAsync(_keyManager.UserConnectionsSetKey(userId), connectionId);

        await UpdateUserStatusAsync(userId, EnumUserPresenceStatus.Online);
    }

    public async Task<Guid?> UserDisconnectedAsync(string connectionId)
    {
        var connectionsKey = _keyManager.ConnectionsHashKey(); // <- Dùng keyManager
        var userIdStr = await _redisDb.HashGetAsync(connectionsKey, connectionId);
        if (string.IsNullOrEmpty(userIdStr)) return null;

        var userId = Guid.Parse(userIdStr);
        var userConnectionsKey = _keyManager.UserConnectionsSetKey(userId); // <- Dùng keyManager

        await _redisDb.HashDeleteAsync(connectionsKey, connectionId);
        await _redisDb.SetRemoveAsync(userConnectionsKey, connectionId);

        if (await _redisDb.SetLengthAsync(userConnectionsKey) == 0)
        {
            await _redisDb.HashDeleteAsync(_keyManager.StatusesHashKey(), userId.ToString()); // <- Dùng keyManager
            // Cập nhật cả SQL khi offline
            _backgroundJobClient.Enqueue<IUserPresenceService>(
                service => service.UpdatePersistentUserStatusAsync(userId, EnumUserPresenceStatus.Offline)
            );
        }
        return userId;
    }

    public async Task UpdateUserStatusAsync(Guid userId, EnumUserPresenceStatus status)
    {
        await _redisDb.HashSetAsync(_keyManager.StatusesHashKey(), userId.ToString(), status.ToString());

        _backgroundJobClient.Enqueue<IUserPresenceService>(
            service => service.UpdatePersistentUserStatusAsync(userId, status)
        );
    }

    public async Task<EnumUserPresenceStatus> GetUserStatusAsync(Guid userId)
    {
        var statusStr = await _redisDb.HashGetAsync(_keyManager.StatusesHashKey(), userId.ToString()); 
        if (string.IsNullOrEmpty(statusStr)) return EnumUserPresenceStatus.Offline;

        return Enum.Parse<EnumUserPresenceStatus>(statusStr!); 
    }

    public async Task<IEnumerable<Guid>> GetOnlineUserIdsAsync()
    {
        var onlineUsers = await _redisDb.HashKeysAsync(_keyManager.StatusesHashKey());
        return onlineUsers.Select(u => Guid.Parse(u.ToString()));
    }

    public async Task<Dictionary<Guid, EnumUserPresenceStatus>> GetStatusesForUsersAsync(IEnumerable<Guid> userIds)
    {
        var userIdsAsRedisVal = userIds.Select(id => (RedisValue)id.ToString()).ToArray();
        var statuses = await _redisDb.HashGetAsync(_keyManager.StatusesHashKey(), userIdsAsRedisVal);

        var result = new Dictionary<Guid, EnumUserPresenceStatus>();
        for (int i = 0; i < userIds.Count(); i++)
        {
            var userId = userIds.ElementAt(i);
            var statusStr = statuses[i];

            var status = string.IsNullOrEmpty(statusStr)
                ? EnumUserPresenceStatus.Offline
                : Enum.Parse<EnumUserPresenceStatus>(statusStr);

            result[userId] = status;
        }
        return result;
    }

    [DisplayName("Update User Presence Status in SQL Server for User: {0}")]
    public async Task UpdatePersistentUserStatusAsync(Guid userId, EnumUserPresenceStatus status)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user != null && user.PresenceStatus != status)
        {
            user.PresenceStatus = status;
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
