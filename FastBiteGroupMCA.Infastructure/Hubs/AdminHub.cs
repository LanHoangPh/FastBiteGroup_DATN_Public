using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FastBiteGroupMCA.Infastructure.Hubs;
[Authorize(Roles = "Admin")]
public class AdminHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        return base.OnDisconnectedAsync(exception);
    }
}
