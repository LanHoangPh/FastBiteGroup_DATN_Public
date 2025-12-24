using FastBiteGroupMCA.Application.DTOs.Admin.AdminNotifications;
using FastBiteGroupMCA.Application.DTOs.Notification;
using FastBiteGroupMCA.Infastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace FastBiteGroupMCA.Infastructure.Services
{
    public class AdminNotificationService : IAdminNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMongoCollection<Notifications> _notificationCol;
        private readonly IHubContext<AdminHub> _adminHubContext;
        private readonly ILogger<NotificationService> _logger;

        public AdminNotificationService(
            IUnitOfWork uow,
            IMongoDatabase mongoDb,
            ILogger<NotificationService> logger,
            IHubContext<AdminHub> adminHubContext)
        {
            _unitOfWork = uow;
            _notificationCol = mongoDb.GetCollection<Notifications>("notifications");
            _logger = logger;
            _adminHubContext = adminHubContext;
        }

        public async Task<ApiResponse<bool>> EnqueueBroadcastAsync(CreateAnnouncementRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ContentPreview))
                return ApiResponse<bool>.Fail("INVALID_CONTENT", "Nội dung thông báo không được để trống.");

            try
            {
                await BroadcastAsync(dto);
                return ApiResponse<bool>.Ok(true, "Thông báo đã được gửi tới tất cả người dùng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi broadcast notification.");
                return ApiResponse<bool>.Fail("SEND_FAILED", "Đã xảy ra lỗi khi gửi thông báo.");
            }
        }

        public async Task BroadcastAsync(CreateAnnouncementRequestDTO dto)
        {
            _logger.LogInformation("[Notification] Broadcast start: {Preview}", dto.ContentPreview);

            var userIds = await _unitOfWork.Users.GetQueryable()
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            _logger.LogInformation("[Notification] Found {Count} active users", userIds.Count);

            var docs = userIds.Select(id => new Notifications
            {
                UserId = id,
                Type = EnumNotificationType.SystemAnnouncement,
                ContentPreview = dto.ContentPreview,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedObject = string.IsNullOrWhiteSpace(dto.NavigateUrl) ? null : new RelatedObjectInfo
                {
                    ObjectType = EnumNotificationObjectType.ExternalLink,
                    NavigateUrl = dto.NavigateUrl
                }
            }).ToList();

            if (docs.Count > 0)
            {
                await _notificationCol.InsertManyAsync(docs);
                _logger.LogInformation("[Notification] Inserted {Count} notifications", docs.Count);
            }
            else
            {
                _logger.LogWarning("[Notification] No users to send notification to.");
            }

            // (Optional) Gửi sự kiện SignalR nếu muốn
        }

        public async Task CreateAndBroadcastNotificationAsync(EnumAdminNotificationType type, string message, string? linkTo = null, Guid? triggeredByUserId = null)
        {
            try
            {
                var notification = new AdminNotifications
                {
                    NotificationType = type,
                    Message = message,
                    LinkTo = linkTo,
                    TriggeredByUserId = triggeredByUserId
                };
                await _unitOfWork.AdminNotifications.AddAsync(notification); 
                await _unitOfWork.SaveChangesAsync();

                await _adminHubContext.Clients.Group("Admins")
                    .SendAsync("NewAdminNotification", new
                    {
                        notification.Id,
                        notification.Message,
                        notification.LinkTo,
                        notification.Timestamp
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create and broadcast admin notification.");
            }
        }

        public async Task<ApiResponse<PagedResult<AdminNotificationDto>>> GetNotificationsAsync(GetAdminNotificationsParams request)
        {
            var query = _unitOfWork.AdminNotifications.GetQueryable();

            // Áp dụng bộ lọc
            if (request.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == request.IsRead.Value);
            }
            // --- BỔ SUNG LOGIC LỌC THEO LOẠI THÔNG BÁO ---
            if (request.NotificationType.HasValue)
            {
                query = query.Where(n => n.NotificationType == request.NotificationType.Value);
            }

            // Sắp xếp theo mới nhất
            var sortedQuery = query.OrderByDescending(n => n.Timestamp);

            // Chiếu sang DTO và phân trang
            var pagedResult = await sortedQuery
                .Select(n => new AdminNotificationDto
                {
                    Id = n.Id,
                    NotificationType = n.NotificationType,
                    Message = n.Message,
                    LinkTo = n.LinkTo,
                    IsRead = n.IsRead,
                    Timestamp = n.Timestamp,
                    TriggeredByUserId = n.TriggeredByUserId,
                    TriggeredByUserName = n.TriggeredByUser!.FullName // Lấy tên từ navigation property
                })
                .ToPagedResultAsync(request.PageNumber, request.PageSize);

            return ApiResponse<PagedResult<AdminNotificationDto>>.Ok(pagedResult);
        }

        public async Task<ApiResponse<object>> MarkAsReadAsync(long notificationId)
        {
            var notification = await _unitOfWork.AdminNotifications.GetByIdAsync(notificationId);

            if (notification == null)
                return ApiResponse<object>.Fail("NOTIFICATION_NOT_FOUND", "Không tìm thấy thông báo.");

            if (notification.IsRead)
                return ApiResponse<object>.Ok(null, "Thông báo đã được đánh dấu đọc trước đó.");

            notification.IsRead = true;
            _unitOfWork.AdminNotifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<object>.Ok(null, "Đánh dấu đọc thành công.");
        }

        public async Task<ApiResponse<object>> MarkAllAsReadAsync()
        {
            var rowsAffected = await _unitOfWork.AdminNotifications.GetQueryable()
                .Where(n => !n.IsRead)
                .ExecuteUpdateAsync(updates => updates.SetProperty(n => n.IsRead, true));

            _logger.LogInformation("Marked {Count} admin notifications as read.", rowsAffected);

            return ApiResponse<object>.Ok(new { Acknowledged = true, UpdatedCount = rowsAffected }, "Tất cả thông báo đã được đánh dấu đọc.");
        }

        public async Task SendBulkActionCompletionNotificationAsync(Guid adminId, Guid batchId, int totalJobs, string actionType)
        {
            // Đếm xem có bao nhiêu log thành công cho batch này
            // (Giả sử log chỉ được ghi khi hành động thành công)
            var successCount = await _unitOfWork.AdminAuditLogs.GetQueryable()
                .CountAsync(l => l.BatchId == batchId);

            var summaryMessage = $"Hành động '{actionType}' đã hoàn tất. {successCount}/{totalJobs} tác vụ thành công.";

            // Đẩy thông báo qua SignalR đến đúng Admin đã thực hiện hành động
            await _adminHubContext.Clients.User(adminId.ToString())
                .SendAsync("BulkActionCompleted", new { message = summaryMessage });
        }
        public async Task SendExportReadyNotificationAsync(Guid adminId, string fileName, string fileUrl)
        {
            var message = $"File export '{fileName}' của bạn đã sẵn sàng.";

            // Đẩy qua SignalR đến đúng Admin đã yêu cầu
            await _adminHubContext.Clients.User(adminId.ToString())
                .SendAsync("ExportCompleted", new { message, url = fileUrl });
        }

        public async Task SendExportFailedNotificationAsync(Guid adminId, string fileName, string details)
        {
            var message = $"Rất tiếc, tác vụ xuất file '{fileName}' đã thất bại. Lỗi {details}";

            // Đẩy thông báo lỗi qua SignalR đến đúng Admin đã yêu cầu
            await _adminHubContext.Clients.User(adminId.ToString())
                .SendAsync("ExportFailed", new { message });
        }
    }
}
