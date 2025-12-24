namespace FastBiteGroupMCA.Infastructure.Redis
{
    public class RedisKeyManager : IRedisKeyManager
    {
        private const string AppPrefix = "fbg";
        private const string PresenceModule = "presence";
        private const string PubSubModule = "pubsub";

        public string ConnectionsHashKey() => $"{AppPrefix}:{PresenceModule}:connections";
        public string StatusesHashKey() => $"{AppPrefix}:{PresenceModule}:statuses";
        public string UserConnectionsSetKey(Guid userId) => $"{AppPrefix}:{PresenceModule}:user:{userId}:connections";
        public string SettingsUpdateChannel() => $"{AppPrefix}:{PubSubModule}:settings-updates";
    }
}
