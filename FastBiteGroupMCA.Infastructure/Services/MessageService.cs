using DocumentFormat.OpenXml.Spreadsheet;
using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.Notifications.Templates;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Infastructure.Hubs;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
using MongoDB.Driver;

namespace FastBiteGroupMCA.Infastructure.Services;

public class MessageService : IMessageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagesRepository _messageRepo;
    private readonly ISettingsService _settingsService;
    private readonly IUserPresenceService _userPresenceService;
    private readonly IOneSignalService _oneSignalService;
    private readonly INotificationService _notificationService;
    private readonly IConversationService _conversationService;
    private readonly IMapper _mapper;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MessageService> _logger;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IHubContext<NotificationsHub> _notificationsHubContext;
    //private readonly IDatabase _redisDb;

    public MessageService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ICurrentUser currentUser, 
        ILogger<MessageService> logger, 
        IHubContext<ChatHub> hubContext, 
        IMessagesRepository messagesRepository, 
        ISettingsService settingsService,
        IUserPresenceService userPresenceService,
        IBackgroundJobClient backgroundJobClient,
        IOneSignalService oneSignalService,
        INotificationService notificationService,
        IHubContext<NotificationsHub> hubContext1,
        IConversationService conversationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _logger = logger;
        _hubContext = hubContext;
        _messageRepo = messagesRepository;
        _settingsService = settingsService;
        _userPresenceService = userPresenceService;
        _backgroundJobClient = backgroundJobClient;
        _oneSignalService = oneSignalService;
        _notificationService = notificationService;
        _notificationsHubContext = hubContext1;
        _conversationService = conversationService;
        //_redisDb = redisDb;
    }

    public async Task<ApiResponse<MessageHistoryResponseDTO>> GetMessageHistoryAsync(int conversationId, GetMessagesQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<MessageHistoryResponseDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var isParticipant = await _unitOfWork.ConversationParticipants.GetQueryable().AnyAsync(p => p.ConversationID == conversationId && p.UserID == userId);
        if (!isParticipant)
            return ApiResponse<MessageHistoryResponseDTO>.Fail("FORBIDDEN", "Không có quyền xem tin nhắn.", 403);

        try
        {
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
            if (conversation == null)
                return ApiResponse<MessageHistoryResponseDTO>.Fail("CONVERSATION_NOT_FOUND", "Không tìm thấy cuộc trò chuyện.");

            var userRole = EnumGroupRole.Member;
            if (conversation.ConversationType == EnumConversationType.Group && conversation.ExplicitGroupID.HasValue)
            {
                var membership = await _unitOfWork.GroupMembers.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(gm => gm.GroupID == conversation.ExplicitGroupID.Value && gm.UserID == userId);

                if (membership != null)
                {
                    userRole = membership.Role;
                }
            }

            var limit = Math.Min(query.Limit, 100);

            var messagesFromMongo = await _messageRepo.GetMessagesBeforeAsync(conversationId, query.BeforeMessageId, limit);

            if (!messagesFromMongo.Any())
            {
                return ApiResponse<MessageHistoryResponseDTO>.Ok(new MessageHistoryResponseDTO { HasMore = false });
            }
            var userIdsToFetch = new HashSet<Guid>();
            foreach (var msg in messagesFromMongo)
            {
                if (msg.Sender != null) userIdsToFetch.Add(msg.Sender.UserId);
                if (msg.ReadBy != null)
                {
                    foreach (var reader in msg.ReadBy) userIdsToFetch.Add(reader.UserId);
                }
                if (msg.Reactions != null)
                {
                    foreach (var reaction in msg.Reactions) userIdsToFetch.Add(reaction.UserId);
                }
            }

            var senderIds = messagesFromMongo
                    .Where(m => m.Sender != null)
                    .Select(m => m.Sender!.UserId)
                    .Distinct()
                    .ToList();

            var usersDict = await _unitOfWork.Users.GetQueryable()
                    .Where(u => userIdsToFetch.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

            var senderRolesDict = new Dictionary<Guid, EnumGroupRole>();
            if (conversation.ConversationType == EnumConversationType.Group && conversation.ExplicitGroupID.HasValue && senderIds.Any())
            {
                senderRolesDict = await _unitOfWork.GroupMembers.GetQueryable()
                    .Where(gm => gm.GroupID == conversation.ExplicitGroupID.Value && senderIds.Contains(gm.UserID))
                    .ToDictionaryAsync(gm => gm.UserID, gm => gm.Role);
            }

            var messagesInDisplayOrder = messagesFromMongo.AsEnumerable().Reverse();

            var messageDtos = messagesInDisplayOrder.Select(msg =>
            {
                var dto = _mapper.Map<MessageDTO>(msg);

                dto.Reactions = msg.Reactions?.Select(r => new ReactionDto
                {
                    UserId = r.UserId,
                    ReactionCode = r.ReactionCode,
                    FullName = usersDict.TryGetValue(r.UserId, out var reactor) ? reactor.FullName! : "...",
                    AvatarUrl = usersDict.TryGetValue(r.UserId, out reactor) ? reactor.AvatarUrl : null
                }).ToList();

                dto.ReadBy = msg.ReadBy?.Select(r => new ReadReceiptDto
                {
                    UserId = r.UserId,
                    ReadAt = r.ReadAt,
                    FullName = usersDict.TryGetValue(r.UserId, out var reader) ? reader.FullName! : "...",
                    AvatarUrl = usersDict.TryGetValue(r.UserId, out reader) ? reader.AvatarUrl : null
                }).ToList();

                dto.IsMine = (msg.Sender?.UserId == userId);


                if (msg.Sender != null)
                {
                    dto.CanEdit = (msg.Sender.UserId == userId);
                    dto.CanDelete = (msg.Sender.UserId == userId) || (userRole > EnumGroupRole.Member);
                    dto.SenderRoleInGroup = senderRolesDict.GetValueOrDefault(msg.Sender.UserId);
                }

                return dto;
            }).ToList();

            var oldestMessageIdInBatch = messagesFromMongo.Last().Id;
            bool hasMore = await _messageRepo.HasOlderMessagesAsync(conversationId, oldestMessageIdInBatch);

            var response = new MessageHistoryResponseDTO
            {
                Messages = messageDtos,
                HasMore = hasMore,
                NextCursor = hasMore ? oldestMessageIdInBatch : null
            };

            return ApiResponse<MessageHistoryResponseDTO>.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy lịch sử tin nhắn cho cuộc trò chuyện {ConversationId}", conversationId);
            return ApiResponse<MessageHistoryResponseDTO>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    public async Task<ApiResponse<MessageDTO>> SendMessageAsync(int conversationId, SendMessageDTO dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var senderGuid))
            return ApiResponse<MessageDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        if (string.IsNullOrWhiteSpace(dto.Content) && (dto.AttachmentFileIds == null || !dto.AttachmentFileIds.Any()))
            return ApiResponse<MessageDTO>.Fail("InvalidMessage", "Tin nhắn phải có nội dung hoặc tệp đính kèm.");

        var forbiddenKeywordsCsv = _settingsService.Get<string>(SettingKeys.ForbiddenKeywords, "Fuck");
        if (!string.IsNullOrEmpty(forbiddenKeywordsCsv))
        {
            var forbiddenWords = forbiddenKeywordsCsv.Split(',').Select(w => w.Trim().ToLower());
            if (forbiddenWords.Any(word => !string.IsNullOrEmpty(word) && dto.Content.ToLower().Contains(word)))
            {
                return ApiResponse<MessageDTO>.Fail("FORBIDDEN_CONTENT", "Tin nhắn của bạn chứa nội dung không phù hợp.");
            }
        }

        try
        {
            var senderData = await _unitOfWork.ConversationParticipants.GetQueryable()
            .Where(p => p.ConversationID == conversationId && p.UserID == senderGuid)
            .Select(p => new {
                User = p.User,
                RoleInGroup = p.Conversation.ConversationType == EnumConversationType.Group
                    ? p.Conversation.Group.Members.Where(m => m.UserID == senderGuid).Select(m => (EnumGroupRole?)m.Role).FirstOrDefault()
                    : null
            })
            .FirstOrDefaultAsync();

            if (senderData?.User == null)
                return ApiResponse<MessageDTO>.Fail("Forbidden", "Bạn không có quyền gửi tin nhắn trong cuộc trò chuyện này.");

            var sender = senderData.User;
            var senderRole = senderData.RoleInGroup;

            var attachments = new List<AttachmentInfo>();
            if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
            {
                var files = await _unitOfWork.SharedFiles.GetQueryable()
                .Where(f => dto.AttachmentFileIds.Contains(f.FileID) && f.UploadedByUserID == senderGuid)
                .ToListAsync();

                if (files.Count != dto.AttachmentFileIds.Count)
                    return ApiResponse<MessageDTO>.Fail("InvalidAttachments", "Một hoặc nhiều file đính kèm không hợp lệ.");

                var videoOrAudioCount = files.Count(f => f.FileType != null && (f.FileType.StartsWith("video/") || f.FileType.StartsWith("audio/")));
                if (videoOrAudioCount > 1 || (videoOrAudioCount > 0 && files.Count > 1))
                {
                    return ApiResponse<MessageDTO>.Fail("INVALID_ATTACHMENT_COMBINATION", "Chỉ được phép gửi một file video hoặc audio duy nhất mỗi lần.");
                }
                attachments = _mapper.Map<List<AttachmentInfo>>(files);
            }

            var newMessage = new Messages
            {
                ConversationId = conversationId,
                Content = dto.Content ?? "",
                Sender = new SenderInfo
                {
                    UserId = sender.Id,
                    AvatarUrl = sender.AvatarUrl,
                    DisplayName = sender.FullName!
                },
                SentAt = DateTime.UtcNow,
                MessageType = attachments.Any() ? DetermineMessageType(attachments.First().FileType) : EnumMessageType.Text,
                Attachments = attachments.Any() ? attachments : null,
            };

            if (!string.IsNullOrEmpty(dto.ParentMessageId))
            {
                var parentMessage = await _messageRepo.GetByIdAsync(dto.ParentMessageId);
                if (parentMessage != null && parentMessage.ConversationId == conversationId)
                {
                    newMessage.ParentMessageId = parentMessage.Id;
                    newMessage.ParentMessage = new ParentMessageInfo
                    {
                        SenderName = parentMessage.Sender.DisplayName,
                        ContentSnippet = GetMessagePreview(parentMessage) 
                    };
                }
            }

            await _messageRepo.InsertOneAsync(newMessage);

            _backgroundJobClient.Enqueue<IMessageService>(service =>
                service.ProcessNewMessageSideEffectsAsync(newMessage.Id, conversationId));

            var messageDto = _mapper.Map<MessageDTO>(newMessage);
            messageDto.IsMine = true;
            messageDto.CanEdit = true;  
            messageDto.CanDelete = true;


            if (senderRole.HasValue)
            {
                messageDto.SenderRoleInGroup = senderRole.Value;
            }

            _logger.LogInformation("Message {MessageId} enqueued for processing in conversation {ConversationId}", newMessage.Id, conversationId);

            return ApiResponse<MessageDTO>.Ok(messageDto, "Tin nhắn đã được gửi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message in conversation {ConversationId}", conversationId);
            throw;
        }
    }
    public async Task ProcessNewMessageSideEffectsAsync(string messageId, int conversationId)
    {
        var message = await _messageRepo.GetByIdAsync(messageId);
        if (message == null) return;

        var participantsToUpdate = await _unitOfWork.ConversationParticipants.GetQueryable()
        .Where(p => p.ConversationID == conversationId && p.IsArchived)
        .ToListAsync();

        if (participantsToUpdate.Any())
        {
            foreach (var participant in participantsToUpdate)
            {
                participant.IsArchived = false;
            }
        }

        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation != null)
        {
            conversation.LastMessagePreview = GetMessagePreview(message);
            conversation.LastMessageTimestamp = message.SentAt;
            conversation.LastMessageSenderName = message.Sender?.DisplayName;
            conversation.UpdatedAt = message.SentAt;
            await _unitOfWork.SaveChangesAsync();
        }

        var allParticipants = await _unitOfWork.ConversationParticipants.GetQueryable()
            .Where(p => p.ConversationID == conversationId)
            .Select(p => p.User)
            .ToListAsync();

        var senderRole = EnumGroupRole.Member; 
        if (conversation.ConversationType == EnumConversationType.Group && conversation.ExplicitGroupID.HasValue && message.Sender != null)
        {
            var senderMembership = await _unitOfWork.GroupMembers.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(gm => gm.GroupID == conversation.ExplicitGroupID.Value && gm.UserID == message.Sender.UserId);
            if (senderMembership != null)
            {
                senderRole = senderMembership.Role;
            }
        }

        foreach (var participant in allParticipants)
        {
            await _notificationsHubContext.Clients.User(participant.Id.ToString())
            .SendAsync("ConversationsShouldRefresh");
            _logger.LogInformation("Sent real-time ConversationsShouldRefresh to user {UserId}", participant.Id);

            var messageDto = _mapper.Map<MessageDTO>(message);

            messageDto.IsMine = (participant.Id == message.Sender?.UserId);

            if (message.Sender != null)
            {
                var participantRoleInGroup = (await _unitOfWork.GroupMembers.GetQueryable().AsNoTracking()
                    .FirstOrDefaultAsync(gm => gm.GroupID == conversation.ExplicitGroupID && gm.UserID == participant.Id))?.Role ?? EnumGroupRole.Member;

                messageDto.CanEdit = (participant.Id == message.Sender.UserId);
                messageDto.CanDelete = (participant.Id == message.Sender.UserId) || (participantRoleInGroup > EnumGroupRole.Member);

                if (conversation.ConversationType == EnumConversationType.Group)
                {
                    messageDto.SenderRoleInGroup = senderRole;
                }
            }

            await _hubContext.Clients.User(participant.Id.ToString())
                .SendAsync("ReceiveMessage", messageDto);

            _logger.LogInformation("Sent real-time message {MessageId} to user {UserId} and message {messageDto}", message.Id, participant.Id, messageDto);

            if (participant.Id != message.Sender?.UserId)
            {
                var presenceStatus = await _userPresenceService.GetUserStatusAsync(participant.Id);
                var notificationMessage = $"{message.Sender?.DisplayName}: {GetMessagePreview(message)}";

                if (presenceStatus == EnumUserPresenceStatus.Offline)
                {
                    await _oneSignalService.SendNotificationToUserAsync(notificationMessage, participant.Id);
                }
                else
                {
                    var eventData = new NewMessageNotificationEventData(message, conversation);
                    await _notificationService.DispatchNotificationAsync<NewMessageNotificationTemplate, NewMessageNotificationEventData>(
                        participant.Id,
                        eventData
                    );
                }
            }
        }
    }


    public async Task SendSystemMessageAsync(int conversationId, string content)
    {
        var systemMessage = new Messages
        {
            ConversationId = conversationId,
            Content = content,
            MessageType = EnumMessageType.SystemNotification,
            SentAt = DateTime.UtcNow,
            Sender = null 
        };

        await _messageRepo.InsertOneAsync(systemMessage);

        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation != null)
        {
            conversation.LastMessagePreview = content;
            conversation.LastMessageTimestamp = systemMessage.SentAt;
            conversation.LastMessageSenderName = null;
            conversation.UpdatedAt = systemMessage.SentAt;

            await _unitOfWork.SaveChangesAsync();
        }

        var messageDto = _mapper.Map<MessageDTO>(systemMessage);
        var groupName = $"conversation_{conversationId}";
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);
    }

    private string GetMessagePreview(Messages message)
    {
        const int previewLength = 100;
        return message.MessageType switch
        {
            EnumMessageType.Image => "Đã gửi một ảnh",
            EnumMessageType.Video => "Đã gửi một video",
            EnumMessageType.File => "Đã gửi một tệp",
            EnumMessageType.Audio => "Đã gửi một tin nhắn thoại",
            EnumMessageType.Poll => "Đã tạo một cuộc bình chọn",
            EnumMessageType.VideoCall => "Cuộc gọi video",
            _ => message.Content.Length > previewLength
                 ? message.Content.Substring(0, previewLength) + "..."
                 : message.Content
        };
    }
    private EnumMessageType DetermineMessageType(string fileType)
    {
        var mainType = fileType.Split('/')[0].ToLower();
        return mainType switch
        {
            "image" => EnumMessageType.Image,
            "video" => EnumMessageType.Video,
            "audio" => EnumMessageType.Audio,
            _ => EnumMessageType.File
        };
    }

    public async Task<ApiResponse<object>> DeleteMessageAsync(int conversationId, string messageId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        try
        {
            var message = await _messageRepo.GetByIdAsync(messageId);
            if (message == null || message.ConversationId != conversationId)
                return ApiResponse<object>.Fail("NOT_FOUND", "Không tìm thấy tin nhắn.", 404);

            if (message.IsDeleted)
                return ApiResponse<object>.Ok(null, "Tin nhắn đã được thu hồi trước đó.");

            var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
            if (conversation == null)
                return ApiResponse<object>.Fail("NOT_FOUND", "Không tìm thấy cuộc trò chuyện.");

            var userRole = EnumGroupRole.Member; 
            if (conversation.ConversationType == EnumConversationType.Group && conversation.ExplicitGroupID.HasValue)
            {
                var membership = await _unitOfWork.GroupMembers.GetQueryable().AsNoTracking()
                    .FirstOrDefaultAsync(gm => gm.GroupID == conversation.ExplicitGroupID.Value && gm.UserID == userId);
                if (membership != null) userRole = membership.Role;
            }

            bool isAuthor = message.Sender?.UserId == userId;
            bool isGroupAdminOrMod = userRole > EnumGroupRole.Member;

            if (!isAuthor && !isGroupAdminOrMod)
                return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền xóa tin nhắn này.", 403);

            var updateFilter = Builders<Messages>.Filter.Eq(m => m.Id, messageId);
            var updateAction = Builders<Messages>.Update
                .Set(m => m.IsDeleted, true)
                .Set(m => m.DeletedAt, DateTime.UtcNow) 
                .Set(m => m.MessageType, EnumMessageType.Delete)
                .Set(m => m.Content, "[Tin nhắn đã được thu hồi]")
                .Unset(m => m.Attachments);

            await _messageRepo.UpdateOneAsync(updateFilter, updateAction);

            _backgroundJobClient.Enqueue<IMessageService>(s => s.BroadcastMessageDeletionAsync(conversationId, messageId));

            return ApiResponse<object>.Ok(null, "Thu hồi tin nhắn thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thu hồi tin nhắn {MessageId}", messageId);
            return ApiResponse<object>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }
    public async Task BroadcastMessageDeletionAsync(int conversationId, string messageId)
    {
        var groupName = $"conversation_{conversationId}";
        await _hubContext.Clients.Group(groupName)
            .SendAsync("MessageDeleted", conversationId, messageId);

        _logger.LogInformation("Broadcasted delete event for message {MessageId} in conversation {ConversationId}", messageId, conversationId);
    }

    public async Task<ApiResponse<ToggleReactionResponseDto>> ToggleReactionAsync(int conversationId, string messageId, ToggleReactionDto dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<ToggleReactionResponseDto>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        if (!await _unitOfWork.ConversationParticipants.GetQueryable()
            .AnyAsync(p => p.ConversationID == conversationId && p.UserID == userId))
            return ApiResponse<ToggleReactionResponseDto>.Fail("FORBIDDEN", "Bạn không có quyền tương tác.", 403);

        var message = await _messageRepo.GetByIdAsync(messageId);
        if (message == null || message.ConversationId != conversationId)
            return ApiResponse<ToggleReactionResponseDto>.Fail("MESSAGE_NOT_FOUND", "Không tìm thấy tin nhắn.", 404);

        var existingReaction = message.Reactions?.FirstOrDefault(r => r.UserId == userId && r.ReactionCode == dto.ReactionCode);

        var updateFilter = Builders<Messages>.Filter.Eq(m => m.Id, messageId);
        UpdateDefinition<Messages> updateAction;

        if (existingReaction != null)
        {
            updateAction = Builders<Messages>.Update.PullFilter(m => m.Reactions,
                r => r.UserId == userId && r.ReactionCode == dto.ReactionCode);
        }
        else
        {
            var newReaction = new Reaction { UserId = userId, ReactionCode = dto.ReactionCode, ReactedAt = DateTime.UtcNow };
            updateAction = Builders<Messages>.Update.AddToSet(m => m.Reactions, newReaction);
        }

        var updatedMessage = await _messageRepo.FindOneAndUpdateAsync(updateFilter, updateAction);
        var newReactions = updatedMessage?.Reactions ?? new List<Reaction>();

        _backgroundJobClient.Enqueue<IMessageService>(service =>
            service.BroadcastReactionUpdateAsync(conversationId, messageId, newReactions));

        var responseDto = new ToggleReactionResponseDto
        {
            MessageId = messageId,
            NewReactions = newReactions
        };
        return ApiResponse<ToggleReactionResponseDto>.Ok(responseDto);
    }

    public async Task BroadcastReactionUpdateAsync(int conversationId, string messageId, List<Reaction> newReactions)
    {
        var userIds = newReactions.Select(r => r.UserId).Distinct().ToList();

        var usersDict = new Dictionary<Guid, AppUser>();
        if (userIds.Any())
        {
            usersDict = await _unitOfWork.Users.GetQueryable()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
        }

        var reactionsDto = newReactions.Select(r => new ReactionDto
        {
            UserId = r.UserId,
            ReactionCode = r.ReactionCode,
            FullName = usersDict.TryGetValue(r.UserId, out var user) ? user.FullName! : "Người dùng không xác định",
            AvatarUrl = usersDict.TryGetValue(r.UserId, out user) ? user.AvatarUrl : null
        }).ToList();
        var groupName = $"conversation_{conversationId}";

        await _hubContext.Clients.Group(groupName)
            .SendAsync("MessageReactionsUpdated", messageId, reactionsDto);
    }

    public async Task<ApiResponse<PagedResult<MessageDTO>>> SearchMessagesAsync(int conversationId, SearchMessagesQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PagedResult<MessageDTO>>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var isParticipant = await _unitOfWork.ConversationParticipants.GetQueryable().AnyAsync(p => p.ConversationID == conversationId && p.UserID == userId);
        if (!isParticipant)
            return ApiResponse<PagedResult<MessageDTO>>.Fail("FORBIDDEN", "Không có quyền xem tin nhắn.", 403);
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null)
            return ApiResponse<PagedResult<MessageDTO>>.Fail("CONVERSATION_NOT_FOUND", "Không tìm thấy cuộc trò chuyện.");

        var domainResult = await _messageRepo.SearchMessagesAsync(conversationId, query.Term, query.PageNumber, query.PageSize);

        var enrichedDtos = await EnrichMessageDtos(domainResult.Items, userId, conversation);

        var finalResult = new PagedResult<MessageDTO>(
            enrichedDtos,
            domainResult.TotalRecords,
            query.PageNumber,
            query.PageSize
        );

        return ApiResponse<PagedResult<MessageDTO>>.Ok(finalResult);
    }
    /// <summary>
    /// (Helper) Làm giàu một danh sách tin nhắn với các thông tin bổ sung như
    /// chi tiết người dùng, vai trò, và các cờ quyền hạn.
    /// </summary>
    private async Task<List<MessageDTO>> EnrichMessageDtos(
        IReadOnlyList<Messages> messages,
        Guid currentUserId,
        Conversation conversation)
    {
        if (!messages.Any())
        {
            return new List<MessageDTO>();
        }

        var userIdsToFetch = new HashSet<Guid>();
        foreach (var msg in messages)
        {
            if (msg.Sender != null) userIdsToFetch.Add(msg.Sender.UserId);
            if (msg.ReadBy != null)
            {
                foreach (var reader in msg.ReadBy) userIdsToFetch.Add(reader.UserId);
            }
            if (msg.Reactions != null)
            {
                foreach (var reaction in msg.Reactions) userIdsToFetch.Add(reaction.UserId);
            }
        }

        var usersDict = await _unitOfWork.Users.GetQueryable()
            .Where(u => userIdsToFetch.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var participantRolesDict = new Dictionary<Guid, EnumGroupRole>();
        if (conversation.ConversationType == EnumConversationType.Group && conversation.ExplicitGroupID.HasValue)
        {
            participantRolesDict = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => gm.GroupID == conversation.ExplicitGroupID.Value && userIdsToFetch.Contains(gm.UserID))
                .ToDictionaryAsync(gm => gm.UserID, gm => gm.Role);
        }

        var currentUserRole = participantRolesDict.GetValueOrDefault(currentUserId, EnumGroupRole.Member);

        var messageDtos = messages.Select(msg =>
        {
            var dto = _mapper.Map<MessageDTO>(msg);

            dto.Reactions = msg.Reactions?.Select(r => new ReactionDto
            {
                UserId = r.UserId,
                ReactionCode = r.ReactionCode,
                FullName = usersDict.TryGetValue(r.UserId, out var reactor) ? reactor.FullName! : "...",
                AvatarUrl = usersDict.TryGetValue(r.UserId, out reactor) ? reactor.AvatarUrl : null
            }).ToList();

            dto.ReadBy = msg.ReadBy?.Select(r => new ReadReceiptDto
            {
                UserId = r.UserId,
                ReadAt = r.ReadAt,
                FullName = usersDict.TryGetValue(r.UserId, out var reader) ? reader.FullName! : "...",
                AvatarUrl = usersDict.TryGetValue(r.UserId, out reader) ? reader.AvatarUrl : null
            }).ToList() ?? new List<ReadReceiptDto>();

            dto.IsMine = (msg.Sender?.UserId == currentUserId);
            if (msg.Sender != null)
            {
                dto.CanEdit = (msg.Sender.UserId == currentUserId);
                dto.CanDelete = (msg.Sender.UserId == currentUserId) || (currentUserRole > EnumGroupRole.Member);
                dto.SenderRoleInGroup = participantRolesDict.GetValueOrDefault(msg.Sender.UserId);
            }

            return dto;
        }).ToList();

        return messageDtos;
    }

    public async Task<ApiResponse<MessageContextResponseDto>> GetMessageContextAsync(int conversationId, GetMessageContextQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<MessageContextResponseDto>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var isParticipant = await _unitOfWork.ConversationParticipants.GetQueryable().AnyAsync(p => p.ConversationID == conversationId && p.UserID == userId);
        if (!isParticipant)
            return ApiResponse<MessageContextResponseDto>.Fail("FORBIDDEN", "Không có quyền xem tin nhắn.", 403);
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
        if (conversation == null)
            return ApiResponse<MessageContextResponseDto>.Fail("CONVERSATION_NOT_FOUND", "Không tìm thấy cuộc trò chuyện.");

        var count = query.PageSize / 2;
        var messagesFromMongo = await _messageRepo.GetMessageContextAsync(conversationId, query.MessageId, count, count);
        if (!messagesFromMongo.Any())
            return ApiResponse<MessageContextResponseDto>.Fail("NOT_FOUND", "Không tìm thấy ngữ cảnh tin nhắn.");

        var enrichedDtos = await EnrichMessageDtos(messagesFromMongo, userId, conversation);

        bool hasOlder = messagesFromMongo.First().Id != enrichedDtos.First().Id || await _messageRepo.HasOlderMessagesAsync(conversationId, enrichedDtos.First().Id);
        bool hasNewer = messagesFromMongo.Last().Id != enrichedDtos.Last().Id || await _messageRepo.HasNewerMessagesAsync(conversationId, enrichedDtos.Last().Id); 

        var response = new MessageContextResponseDto
        {
            Messages = enrichedDtos,
            TargetMessageId = query.MessageId,
            HasOlderMessages = hasOlder,
            HasNewerMessages = hasNewer
        };

        return ApiResponse<MessageContextResponseDto>.Ok(response);
    }
}
