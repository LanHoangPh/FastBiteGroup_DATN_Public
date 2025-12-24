namespace FastBiteGroupMCA.Infastructure.Redis
{
    public interface IRedisKeyManager
    {
        // Presence Module Keys
        string ConnectionsHashKey();
        string StatusesHashKey();
        string UserConnectionsSetKey(Guid userId);

        // Pub/Sub Module Keys
        string SettingsUpdateChannel();

        // Có thể thêm các key cho việc caching sau này
        // string UserProfileCacheKey(Guid userId); 
    }
}
