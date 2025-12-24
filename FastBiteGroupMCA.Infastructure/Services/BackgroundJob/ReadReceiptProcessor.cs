using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.IServices.BackgroundJob;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Infastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FastBiteGroupMCA.Infastructure.Services.BackgroundJob;

public class ReadReceiptProcessor : IReadReceiptProcessor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagesRepository _messageRepo;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IHubContext<NotificationsHub> _notificationsHubContext;
    private readonly IConversationService _conversationService;
    private readonly IMapper _mapper;
    private readonly ILogger<ReadReceiptProcessor> _logger;

    public ReadReceiptProcessor(
        IUnitOfWork unitOfWork, 
        IMessagesRepository messageRepo,
        IHubContext<ChatHub> hubContext,
        IMapper mapper,
        ILogger<ReadReceiptProcessor> logger,
        IConversationService conversationService,
        IHubContext<NotificationsHub> notificationsHubContext)
    {
        _unitOfWork = unitOfWork;
        _messageRepo = messageRepo;
        _hubContext = hubContext;
        _mapper = mapper;
        _logger = logger;
        _conversationService = conversationService;
        _notificationsHubContext = notificationsHubContext;
    }

    public async Task ProcessAsync(MarkAsReadDTO dto, Guid readerUserId)
    {
        try
        {
            var readAt = DateTime.UtcNow;
            var readerInfo = new ReadReceiptInfo { UserId = readerUserId, ReadAt = readAt };

            var updatedCount = await _messageRepo.MarkMessagesAsReadAsync(dto.MessageIds, readerInfo);
            if (updatedCount == 0) return; 

            // B. Cập nhật SQL Server
            var participant = await _unitOfWork.ConversationParticipants.GetQueryable()
                .FirstOrDefaultAsync(p => p.ConversationID == dto.ConversationId && p.UserID == readerUserId);

            if (participant != null)
            {
                participant.LastReadTimestamp = readAt;
                _unitOfWork.ConversationParticipants.Update(participant);
                await _unitOfWork.SaveChangesAsync();
            }
            await _notificationsHubContext.Clients.Group($"User_{participant.UserID}")
                    .SendAsync("ConversationsShouldRefresh");

            var reader = await _unitOfWork.Users.GetByIdAsync(readerUserId);
            if (reader == null) return;

            var readerDto = new ReadReceiptDto
            {
                UserId = reader.Id,
                FullName = reader.FullName!,
                AvatarUrl = reader.AvatarUrl,
                ReadAt = readAt
            };

            // --- CẢI TIẾN: Gửi một sự kiện duy nhất chứa mảng các ID ---
            var groupName = $"conversation_{dto.ConversationId}";
            await _hubContext.Clients.Group(groupName)
                .SendAsync("MessagesReadBy", dto.ConversationId, dto.MessageIds, readerDto);
            _logger.LogInformation("Send realtime MessagesReadBy {ConversationId} is Message {Id} ",dto.ConversationId, dto.MessageIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing read receipts for user {UserId} in conversation {ConversationId}", readerUserId, dto.ConversationId);
        }
    }
}
