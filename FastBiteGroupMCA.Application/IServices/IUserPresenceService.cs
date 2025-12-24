using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.IServices;

public interface IUserPresenceService
{
    Task UpdatePersistentUserStatusAsync(Guid userId, EnumUserPresenceStatus status);
    Task UserConnectedAsync(Guid userId, string connectionId);
    Task<Guid?> UserDisconnectedAsync(string connectionId);
    Task UpdateUserStatusAsync(Guid userId, EnumUserPresenceStatus status);
    Task<Dictionary<Guid, EnumUserPresenceStatus>> GetStatusesForUsersAsync(IEnumerable<Guid> userIds);
    Task<EnumUserPresenceStatus> GetUserStatusAsync(Guid userId);
    Task<IEnumerable<Guid>> GetOnlineUserIdsAsync();
}
