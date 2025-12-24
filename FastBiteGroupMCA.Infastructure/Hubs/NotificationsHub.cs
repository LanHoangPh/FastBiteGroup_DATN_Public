using FastBiteGroupMCA.Application.CurrentUserClaim;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FastBiteGroupMCA.Infastructure.Hubs;
[Authorize(Roles = "Customer,VIP")]
public class NotificationsHub : Hub
{
    private readonly INotificationService _notificationService;
    private readonly IUserPresenceService _presenceService;
    private readonly ICurrentUser _currentUser;

    public NotificationsHub(INotificationService notificationService, ICurrentUser currentUser, IUserPresenceService presenceService)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
        _presenceService = presenceService;
    }

    public override async Task OnConnectedAsync()
    {
        if (Guid.TryParse(_currentUser.Id, out var userId))
        {
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
