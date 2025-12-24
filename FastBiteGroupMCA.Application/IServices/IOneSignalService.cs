namespace FastBiteGroupMCA.Application.IServices;

public interface IOneSignalService
{
    Task SendNotificationToUserAsync(string message, Guid userId, string? webUrl = null);
    Task SendNotificationToMultipleUsersAsync(string message, List<Guid> userIds, string? webUrl = null);
}
