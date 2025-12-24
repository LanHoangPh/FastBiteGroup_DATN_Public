using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using FastBiteGroupMCA.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FastBiteGroupMCA.API.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly IUserPresenceService _presenceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PresenceHub> _logger;

    public PresenceHub(IUserPresenceService presenceService, IUnitOfWork unitOfWork, ILogger<PresenceHub> logger)
    {
        _presenceService = presenceService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    private Guid CurrentUserId => Guid.Parse(Context.UserIdentifier!);

    public override async Task OnConnectedAsync()
    {
        await _presenceService.UserConnectedAsync(CurrentUserId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{CurrentUserId}");

        await BroadcastStatusChange(CurrentUserId, EnumUserPresenceStatus.Online);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var disconnectedUserId = await _presenceService.UserDisconnectedAsync(Context.ConnectionId);

        if (disconnectedUserId.HasValue)
        {
            // Broadcast trạng thái Offline cho đúng người dùng đã ngắt kết nối
            await BroadcastStatusChange(disconnectedUserId.Value, EnumUserPresenceStatus.Offline);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToUserStatus(Guid targetUserId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{targetUserId}");
    }

    public async Task UnsubscribeFromUserStatus(Guid targetUserId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{targetUserId}");
    }

    public async Task ChangeMyStatus(string newStatusStr)
    {
        if (!Enum.TryParse<EnumUserPresenceStatus>(newStatusStr, true, out var newStatus))
        {
            _logger.LogWarning("Invalid status value received from client: {StatusValue}", newStatusStr);
            return; 
        }
        if (newStatus == EnumUserPresenceStatus.Online || newStatus == EnumUserPresenceStatus.Offline) return;

        await _presenceService.UpdateUserStatusAsync(CurrentUserId, newStatus);
        await BroadcastStatusChange(CurrentUserId, newStatus);
    }

    private async Task BroadcastStatusChange(Guid userId, EnumUserPresenceStatus status)
    {
        var groupIds = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserID == userId)
            .Select(gm => gm.GroupID.ToString())
            .ToListAsync();

        var groupNames = groupIds.Select(id => $"conversation_{id}").ToArray();
        var dto = new UserStatusChangedDTO { UserId = userId.ToString(), PresenceStatus = status };

        if (groupNames.Any())
        {
            await Clients.Groups(groupNames).SendAsync("UserStatusChanged", dto);
        }
    }
}
