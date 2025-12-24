using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Notification;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Application.Notifications.Templates;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Infastructure.Hubs;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace FastBiteGroupMCA.Infastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationsRepository _notificationsRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOneSignalService _oneSignalService;
    private readonly IUserPresenceService _presenceService;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly IEmailService _emailService; 
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper; 
    private readonly ILogger<NotificationService> _logger;


    public NotificationService(
        INotificationsRepository notificationsRepository, 
        IBackgroundJobClient backgroundJobClient, 
        IHubContext<NotificationsHub> hubContext, 
        IUnitOfWork unitOfWork, IMapper mapper, 
        IEmailService emailService, 
        IServiceProvider serviceProvider, 
        IUserPresenceService userPresenceService, 
        IOneSignalService oneSignalService, 
        ILogger<NotificationService> logger)
    {
        _notificationsRepository = notificationsRepository;
        _backgroundJobClient = backgroundJobClient;
        _hubContext = hubContext;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _serviceProvider = serviceProvider;
        _presenceService = userPresenceService;
        _oneSignalService = oneSignalService;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<NotificationDTO>>> GetMyNotificationsAsync(Guid userId, PaginationParams pageParams)
    {
        Expression<Func<Notifications, bool>> filter = n => n.UserId == userId;
        Expression<Func<Notifications, object>> sort = n => n.CreatedAt;

        var totalRecords = await _notificationsRepository.CountAsync(filter);

        var notifications = await _notificationsRepository.GetPagedAsync(
            filter,
            pageParams.PageNumber,
            pageParams.PageSize,
            sort,
            isDescending: true 
        );

        var notificationDtos = _mapper.Map<List<NotificationDTO>>(notifications);

        var pagedResult = new PagedResult<NotificationDTO>(notificationDtos, totalRecords, pageParams.PageNumber, pageParams.PageSize);
        return ApiResponse<PagedResult<NotificationDTO>>.Ok(pagedResult);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        var unreadCount = await _notificationsRepository.CountAsync(n => n.UserId == userId && !n.IsRead);
        return (int)unreadCount;
    }

    public async Task<ApiResponse<object>> MarkAsReadAsync(string notificationId, Guid userId)
    {
        var success = await _notificationsRepository.FindAndMarkAsReadAsync(notificationId, userId);

        if (!success)
        {

            return ApiResponse<object>.Fail("UpdateFailed", "Không thể cập nhật thông báo. Có thể nó không tồn tại hoặc đã được đọc.");
        }

        var unreadCount = await GetUnreadCountAsync(userId);
        await _hubContext.Clients.User(userId.ToString()).SendAsync("UpdateUnreadCount", unreadCount);

        return ApiResponse<object>.Ok(null!, "Đánh dấu đã đọc thành công.");
    }

    public async Task<ApiResponse<object>> MarkAllAsReadAsync(Guid userId)
    {
        var modified = await _notificationsRepository.MarkAllAsReadAsync(userId);
        await _hubContext.Clients.User(userId.ToString()).SendAsync("UpdateUnreadCount", 0);
        return ApiResponse<object>.Ok(null!, $"Đã đánh dấu {modified} thông báo là đã đọc.");
    }

    public async Task NotifyUserAddedToGroupByAdminAsync(Guid addedUserId, string addedUserName, Guid groupId, string groupName, string systemAdminName)
    {

        var eventData = new UserAddedToGroupByAdminEventData(
        AddedUserName: addedUserName,
        GroupName: groupName,
        GroupId: groupId,
        AdminName: systemAdminName
        );

        await DispatchNotificationAsync<UserAddedToGroupByAdminNotificationTemplate, UserAddedToGroupByAdminEventData>(
            addedUserId,
            eventData
        );

        var adminModeratorsQuery = _unitOfWork.GroupMembers.GetQueryable()
        .Where(m => m.GroupID == groupId && (m.Role == EnumGroupRole.Admin || m.Role == EnumGroupRole.Moderator))
        .Select(m => new { m.User.Id, m.User.Email });

        var adminModerators = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(adminModeratorsQuery);

        foreach (var admin in adminModerators)
        {
            if (admin.Id == addedUserId) continue;

            await DispatchNotificationAsync<AdminNotifiedOfNewMemberTemplate, UserAddedToGroupByAdminEventData>(
                admin.Id,
                eventData
            );
        }

        var adminEmails = adminModerators
            .Where(a => !string.IsNullOrEmpty(a.Email))
            .Select(a => a.Email!)
            .ToList();

        if (adminEmails.Any())
        {
            await _emailService.SendNotifyAdminsUserAddedAsync(adminEmails, addedUserName, groupName, systemAdminName);
        }
        var userToNotify = await _unitOfWork.Users.GetByIdAsync(addedUserId);
        if (userToNotify?.Email != null)
        {
            await _emailService.SendNotifyUserTheyWereAddedAsync(userToNotify.Email, addedUserName, groupName, groupId, systemAdminName);
        }
    }

    public async Task DispatchNotificationAsync<TTemplate, TEventData>(Guid targetUserId, TEventData eventData)
        where TTemplate : class, INotificationTemplate<TEventData>
    {
        var template = _serviceProvider.GetRequiredService<TTemplate>();

        var notificationType = template.NotificationType;
        var contentPreview = template.BuildContent(eventData);
        var relatedObject = template.BuildRelatedObject(eventData);

        await CreateAndSendNotificationInternalAsync(targetUserId, notificationType, contentPreview, relatedObject);
    }

    private async Task CreateAndSendNotificationInternalAsync(Guid targetUserId, EnumNotificationType notificationType, string contentPreview, RelatedObjectInfo? relatedObject)
    {
        try
        {
            var newNotification = new Notifications
            {
                UserId = targetUserId,
                Type = notificationType,
                ContentPreview = contentPreview,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedObject = relatedObject
            };
            await _notificationsRepository.InsertOneAsync(newNotification);

            var userStatus = await _presenceService.GetUserStatusAsync(targetUserId);

            if (userStatus != EnumUserPresenceStatus.Offline)
            {
                await _hubContext.Clients.User(targetUserId.ToString())
                    .SendAsync("ReceiveNewNotification", _mapper.Map<NotificationDTO>(newNotification));
            }
            else
            {
                var url = relatedObject?.NavigateUrl ?? "/";
                _backgroundJobClient.Enqueue<IOneSignalService>(service =>
                    service.SendNotificationToUserAsync(contentPreview, targetUserId, url)
                );
            }

            var unreadCount = await GetUnreadCountAsync(targetUserId);
            await _hubContext.Clients.User(targetUserId.ToString()).SendAsync("UpdateUnreadCount", unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAndSendNotificationInternalAsync");
            return;
        }
    }
}
