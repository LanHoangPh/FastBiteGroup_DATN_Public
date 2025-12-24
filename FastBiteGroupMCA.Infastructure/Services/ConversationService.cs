using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Conversation;
using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.IServices.BackgroundJob;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using Hangfire;

namespace FastBiteGroupMCA.Infastructure.Services;

public class ConversationService : IConversationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IUserPresenceService _presenceService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMapper _mapper;
    private readonly ILogger<ConversationService> _logger;
    private readonly IMessagesRepository _messageRepo;

    public ConversationService(
        IUnitOfWork unitOfWork, 
        ICurrentUser currentUser, 
        ILogger<ConversationService> logger, 
        IMapper mapper, 
        IMessagesRepository messageRepo,
        IUserPresenceService presenceService,
        IBackgroundJobClient backgroundJobClient)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
        _mapper = mapper;
        _messageRepo = messageRepo;
        _presenceService = presenceService;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<ApiResponse<ConversationResponseDto>> FindOrCreateDirectConversationAsync(Guid partnerUserId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<ConversationResponseDto>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ.",401);

        if (currentUserId == partnerUserId)
            return ApiResponse<ConversationResponseDto>.Fail("INVALID_PARTNER", "Không thể tạo cuộc trò chuyện với chính mình.");

        var partner = await _unitOfWork.Users.GetByIdAsync(partnerUserId);
        if (partner == null || partner.IsDeleted)
            return ApiResponse<ConversationResponseDto>.Fail("PARTNER_NOT_FOUND", "Không tìm thấy người dùng đối thoại.", 404);

        if (partner.MessagingPrivacy == EnumMessagingPrivacy.FromSharedGroupMembers)
        {
            var haveSharedGroup = await _unitOfWork.GroupMembers.GetQueryable()
                .AnyAsync(gm1 => gm1.UserID == currentUserId &&
                                 _unitOfWork.GroupMembers.GetQueryable().Any(gm2 => gm2.UserID == partnerUserId && gm2.GroupID == gm1.GroupID));

            if (!haveSharedGroup)
            {
                return ApiResponse<ConversationResponseDto>.Fail(
                    "PRIVACY_RESTRICTION",
                    $"{partner.FullName} hiện không chấp nhận tin nhắn từ người lạ. Bạn cần tham gia ít nhất một nhóm chung để có thể bắt đầu cuộc trò chuyện." 
                );
            }
        }

        var(conversationEntity, wasCreated) = await FindOrCreateDirectConversationInternalAsync(currentUserId, partnerUserId);

        // Nếu conversationEntity được tìm thấy và đã được "bỏ ẩn", lệnh này sẽ lưu thay đổi đó
        await _unitOfWork.SaveChangesAsync();

        if (conversationEntity == null)
            return ApiResponse<ConversationResponseDto>.Fail("CREATION_FAILED", "Không thể tạo hoặc tìm thấy cuộc trò chuyện.");

        var partnerStatus = await _presenceService.GetUserStatusAsync(partnerUserId);

        var responseDto = new ConversationResponseDto
        {
            ConversationId = conversationEntity.ConversationID,
            WasCreated = wasCreated,
            Partner = new ConversationPartnerDto
            {
                UserId = partner.Id,
                FullName = partner.FullName!,
                AvatarUrl = partner.AvatarUrl,
                PresenceStatus = partnerStatus 
            }
        };

        return ApiResponse<ConversationResponseDto>.Ok(responseDto, "Thành công.");
    }

    public async Task<ApiResponse<PagedResult<ConversationListItemDTO>>> GetMyConversationsAsync(GetMyConversationsQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<PagedResult<ConversationListItemDTO>>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ.");

        try
        {

            var conversationsQuery = _unitOfWork.ConversationParticipants.GetQueryable()
            .Where(p => p.UserID == currentUserId && !p.IsArchived)
            .Select(p => p.Conversation); 

            conversationsQuery = conversationsQuery.Where(c => 
                c.ConversationType == EnumConversationType.Direct || 
                (c.ConversationType == EnumConversationType.Group && c.Group != null && !c.Group.IsDeleted)
            );

            if (query.Filter?.ToLower() == "direct")
                conversationsQuery = conversationsQuery.Where(c => c.ConversationType == EnumConversationType.Direct);
            else if (query.Filter?.ToLower() == "group")
                conversationsQuery = conversationsQuery.Where(c => c.ConversationType == EnumConversationType.Group);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim();
                conversationsQuery = conversationsQuery.Where(c =>
                    // Tìm trong tên của Nhóm Chat
                    (c.ConversationType == EnumConversationType.Group && c.Title != null && c.Title.Contains(term)) ||
                    // Hoặc tìm trong tên của người đối thoại (chat 1-1)
                    (c.ConversationType == EnumConversationType.Direct &&
                     c.Participants.Any(p => p.UserID != currentUserId && p.User.FullName != null && p.User.FullName.Contains(term)))
                );
            }

            var orderedQuery = conversationsQuery.OrderByDescending(c => c.LastMessageTimestamp);

            var pagedConversations = await orderedQuery
                .ToPagedResultAsync(query.PageNumber, query.PageSize);

            var conversationItems = pagedConversations.Items;
            var conversationIds = conversationItems.Select(c => c.ConversationID).ToList();

            if (!conversationIds.Any())
            {
                return ApiResponse<PagedResult<ConversationListItemDTO>>.Ok(
                    new PagedResult<ConversationListItemDTO>(new List<ConversationListItemDTO>(), 0, query.PageNumber, query.PageSize));
            }

            var participantsDataTask = _unitOfWork.ConversationParticipants.GetQueryable()
                .Where(p => conversationIds.Contains(p.ConversationID))
                .Select(p => new {
                    p.ConversationID,
                    p.UserID,
                    p.LastReadTimestamp,
                    PartnerUser = p.User 
                })
                .ToListAsync();

            var lastMessagesTask = _messageRepo.GetLastMessageForConversationsAsync(conversationIds);

            await Task.WhenAll(participantsDataTask, lastMessagesTask);

            var participantsData = await participantsDataTask;
            var lastMessagesDict = await lastMessagesTask;

            var partnerIds = participantsData.Where(p => p.UserID != currentUserId).Select(p => p.UserID).Distinct().ToList();
            var presenceStatusDict = partnerIds.Any()
                ? await _presenceService.GetStatusesForUsersAsync(partnerIds)
                : new Dictionary<Guid, EnumUserPresenceStatus>();

            var unreadCountsTask = _messageRepo.GetUnreadCountsForConversationsAsync(conversationIds, currentUserId);

            var unreadCountsDict = await unreadCountsTask;

            var conversationDtos = conversationItems.Select(conv =>
            {
                var dto = new ConversationListItemDTO
                {
                    ConversationId = conv.ConversationID,
                    GroupId = conv.ExplicitGroupID,
                    ConversationType = conv.ConversationType,
                    UnreadCount = (int)(unreadCountsDict.TryGetValue(conv.ConversationID, out var count) ? count : 0)
                };

                if (conv.ConversationType == EnumConversationType.Direct)
                {
                    var partnerData = participantsData.FirstOrDefault(p => p.ConversationID == conv.ConversationID && p.UserID != currentUserId);

                    if (partnerData?.PartnerUser != null)
                    {
                        dto.DisplayName = partnerData.PartnerUser.FullName;
                        dto.AvatarUrl = partnerData.PartnerUser.AvatarUrl;
                        dto.PartnerPresenceStatus = presenceStatusDict.GetValueOrDefault(partnerData.PartnerUser.Id, EnumUserPresenceStatus.Offline);
                    }
                }
                else // Group Chat
                {
                    dto.DisplayName = conv.Title;
                    dto.AvatarUrl = conv.AvatarUrl;
                }

                if (lastMessagesDict.TryGetValue(conv.ConversationID, out var lastMessage))
                {
                    dto.LastMessagePreview = GetMessagePreview(lastMessage);
                    dto.LastMessageType = lastMessage.MessageType;
                    dto.LastMessageTimestamp = lastMessage.SentAt;
                }
                else // Trường hợp cuộc trò chuyện chưa có tin nhắn nào
                {
                    dto.LastMessagePreview = "Chưa có tin nhắn nào.";
                    dto.LastMessageTimestamp = conv.CreatedAt;
                }

                return dto;
            }).ToList();

            var finalPagedResult = new PagedResult<ConversationListItemDTO>(conversationDtos, pagedConversations.TotalRecords, query.PageNumber, query.PageSize);
            return ApiResponse<PagedResult<ConversationListItemDTO>>.Ok(finalPagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách cuộc trò chuyện của người dùng {UserId}", currentUserId);
            return ApiResponse<PagedResult<ConversationListItemDTO>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    // Helper để tạo preview tin nhắn (bạn có thể chuyển nó vào một lớp tiện ích chung)
    private string GetMessagePreview(Messages message)
    {
        const int previewLength = 50;
        if(message.Content == "{\"videoCallSessionId\":\"8784181b-6aab-4fe6-b7dd-658ae85fd251\"}")
        {
            return "Đang có cuộc gọi nhóm ";
        }
        return message.MessageType switch
        {
            EnumMessageType.Image => "Đã gửi một ảnh",
            EnumMessageType.Video => "Đã gửi một video",
            EnumMessageType.File => "Đã gửi một tệp",
            EnumMessageType.Audio => "Đã gửi một tin nhắn thoại",
            _ => message.Content.Length > previewLength
                   ? message.Content.Substring(0, previewLength) + "..."
                   : message.Content
        };
    }

    private async Task<(Conversation?, bool)> FindOrCreateDirectConversationInternalAsync(Guid user1Id, Guid user2Id)
    {
        var existingConversation = await _unitOfWork.Conversations.GetQueryable()
        .Include(c => c.Participants)
        .Where(c => c.ConversationType == EnumConversationType.Direct &&
                     c.Participants.Count == 2 &&
                     c.Participants.Any(p => p.UserID == user1Id) &&
                     c.Participants.Any(p => p.UserID == user2Id))
        .FirstOrDefaultAsync();

        if (existingConversation != null)
        {
            var currentUserParticipant = existingConversation.Participants.FirstOrDefault(p => p.UserID == user1Id);

            if (currentUserParticipant != null && currentUserParticipant.IsArchived)
            {
                currentUserParticipant.IsArchived = false;
            }

            return (existingConversation, false);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var user1 = await _unitOfWork.Users.GetByIdAsync(user1Id);
            var user2 = await _unitOfWork.Users.GetByIdAsync(user2Id);

            var newConversation = new Conversation
            {
                ConversationType = EnumConversationType.Direct,
                Participants = new List<ConversationParticipants>
            {
                new() { User = user1, JoinedAt = DateTime.UtcNow, IsArchived = false },
                new() { User = user2, JoinedAt = DateTime.UtcNow, IsArchived = false }
            }
            };

            await _unitOfWork.Conversations.AddAsync(newConversation);
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            return (newConversation, true); // Tạo mới thành công
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create direct conversation between {User1} and {User2}", user1Id, user2Id);
            return (null, false);
        }
    }

    public async Task<ApiResponse<ConversationDetailDto>> GetConversationDetailsAsync(int conversationId, GetConversationMessagesQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<ConversationDetailDto>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        try
        {
            var conversationSql = await _unitOfWork.Conversations.GetQueryable()
            .AsNoTracking()
            .Where(c => c.ConversationID == conversationId)
            .Select(c => new
            {
                Conversation = c,
                PartnerUser = c.ConversationType == EnumConversationType.Direct
                    ? c.Participants.Select(p => p.User).FirstOrDefault(u => u!.Id != userId)
                    : null,
                CurrentUserRole = c.ConversationType == EnumConversationType.Group
                    ? c.Group!.Members.Where(m => m.UserID == userId).Select(m => (EnumGroupRole?)m.Role).FirstOrDefault()
                    : null
            })
            .FirstOrDefaultAsync();

            if (conversationSql == null)
                return ApiResponse<ConversationDetailDto>.Fail("NOT_FOUND", "Không tìm thấy cuộc trò chuyện.");

            if (!await _unitOfWork.ConversationParticipants.GetQueryable().AnyAsync(p => p.ConversationID == conversationId && p.UserID == userId))
                return ApiResponse<ConversationDetailDto>.Fail("FORBIDDEN", "Bạn không có quyền truy cập.", 403);

            int pageSize = query.PageSize;
            var messagesFromMongo = await _messageRepo.GetMessagesBeforeAsync(conversationId, null, pageSize);
            var totalMessages = await _messageRepo.CountAsync(m => m.ConversationId == conversationId);

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

            // b. Lấy thông tin chi tiết của tất cả user trong MỘT LẦN GỌI DUY NHẤT
            var usersDict = await _unitOfWork.Users.GetQueryable()
                .Where(u => userIdsToFetch.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);


            var senderIds = messagesFromMongo.Where(m => m.Sender != null).Select(m => m.Sender!.UserId).Distinct().ToList();
            var senderRolesDict = new Dictionary<Guid, EnumGroupRole>();
            if (conversationSql.Conversation.ConversationType == EnumConversationType.Group && conversationSql.Conversation.ExplicitGroupID.HasValue && senderIds.Any())
            {
                senderRolesDict = await _unitOfWork.GroupMembers.GetQueryable()
                    .Where(gm => gm.GroupID == conversationSql.Conversation.ExplicitGroupID.Value && senderIds.Contains(gm.UserID))
                    .ToDictionaryAsync(gm => gm.UserID, gm => gm.Role);
            }

            var messageDtos = _mapper.Map<List<MessageDTO>>(messagesFromMongo.AsEnumerable().Reverse());
            foreach (var dto in messageDtos)
            {
                dto.Reactions = dto.Reactions?.Select(r => {
                    if (usersDict.TryGetValue(r.UserId, out var reactor))
                    {
                    }
                    return r;
                }).ToList();

                dto.ReadBy = dto.ReadBy?.Select(r => {
                    if (usersDict.TryGetValue(r.UserId, out var reader))
                    {
                        r.FullName = reader.FullName!;
                        r.AvatarUrl = reader.AvatarUrl;
                    }
                    return r;
                }).ToList();

                dto.IsMine = (dto.Sender?.UserId == userId);
                if (dto.Sender != null)
                {
                    dto.CanEdit = (dto.Sender.UserId == userId);
                    dto.CanDelete = (dto.Sender.UserId == userId) || (conversationSql.CurrentUserRole > EnumGroupRole.Member);
                    dto.SenderRoleInGroup = senderRolesDict.GetValueOrDefault(dto.Sender.UserId);
                }
            }

            var messagesPage = new PagedResult<MessageDTO>(messageDtos, totalMessages, 1, pageSize);

            var responseDto = new ConversationDetailDto
            {
                ConversationId = conversationSql.Conversation.ConversationID,
                GroupId = conversationSql.Conversation.ExplicitGroupID,
                ConversationType = conversationSql.Conversation.ConversationType,
                DisplayName = conversationSql.Conversation.ConversationType == EnumConversationType.Group ? conversationSql.Conversation.Title : conversationSql.PartnerUser?.FullName,
                AvatarUrl = conversationSql.Conversation.ConversationType == EnumConversationType.Group ? conversationSql.Conversation.AvatarUrl : conversationSql.PartnerUser?.AvatarUrl,
                Partner = _mapper.Map<ConversationPartnerDto>(conversationSql.PartnerUser),
                MessagesPage = messagesPage
            };

            if (responseDto.Partner != null)
            {
                responseDto.Partner.PresenceStatus = await _presenceService.GetUserStatusAsync(responseDto.Partner.UserId);
                var partnerId = responseDto.Partner.UserId;

                var presenceStatusTask = _presenceService.GetUserStatusAsync(partnerId);

                var myGroupIds = await _unitOfWork.GroupMembers.GetQueryable()
                    .Where(gm => gm.UserID == userId)
                    .Select(gm => gm.GroupID)
                    .ToListAsync();

                var mutualGroupsCountTask = myGroupIds.Any()
                    ? _unitOfWork.GroupMembers.GetQueryable()
                        .CountAsync(gm => gm.UserID == partnerId && myGroupIds.Contains(gm.GroupID))
                    : Task.FromResult(0);

                await Task.WhenAll(presenceStatusTask, mutualGroupsCountTask);

                responseDto.Partner.PresenceStatus = await presenceStatusTask;
                responseDto.Partner.MutualGroupsCount = await mutualGroupsCountTask;
            }

            return ApiResponse<ConversationDetailDto>.Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết cuộc trò chuyện {ConversationId}", conversationId);
            return ApiResponse<ConversationDetailDto>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    public async Task<ApiResponse<object>> DeleteConversationForCurrentUserAsync(int conversationId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var participantRecord = await _unitOfWork.ConversationParticipants.GetQueryable()
            .Include(p => p.Conversation)
            .FirstOrDefaultAsync(p => p.ConversationID == conversationId && p.UserID == userId);

        if (participantRecord == null)
            return ApiResponse<object>.Fail("NOT_FOUND", "Không tìm thấy cuộc trò chuyện hoặc bạn không phải là thành viên.", 404);

        if (participantRecord.Conversation.ConversationType != EnumConversationType.Direct)
            return ApiResponse<object>.Fail("INVALID_ACTION", "Hành động này chỉ áp dụng cho cuộc trò chuyện 1-1.");

        // Thực hiện hành động "Xóa mềm"
        participantRecord.IsArchived = true;
        await _unitOfWork.SaveChangesAsync();

        var remainingActiveParticipants = await _unitOfWork.ConversationParticipants.GetQueryable()
            .CountAsync(p => p.ConversationID == conversationId && !p.IsArchived);

        if (remainingActiveParticipants == 0)
        {
            _backgroundJobClient.Schedule<IDataRetentionService>(
                service => service.PermanentlyDeleteConversationAsync(conversationId),
                TimeSpan.FromDays(30)
            );
            _logger.LogInformation("All participants have archived conversation {ConversationId}. Scheduled for permanent deletion.", conversationId);
        }

        return ApiResponse<object>.Ok(null, "Đã xóa cuộc trò chuyện khỏi danh sách của bạn.");
    }

    public async Task<ConversationListItemDTO?> BuildConversationListItemDtoForUser(int conversationId, Guid userId)
    {
        try
        {
            var conversationSql = await _unitOfWork.Conversations.GetQueryable()
                .AsNoTracking()
                .Where(c => c.ConversationID == conversationId)
                .Select(c => new
                {
                    Conversation = c,
                    PartnerUser = c.ConversationType == EnumConversationType.Direct
                        ? c.Participants.Select(p => p.User).FirstOrDefault(u => u!.Id != userId)
                        : null,
                    CurrentUserLastRead = c.Participants.FirstOrDefault(p => p.UserID == userId)!.LastReadTimestamp
                })
                .FirstOrDefaultAsync();

            if (conversationSql == null)
            {
                _logger.LogWarning("Could not build DTO. Conversation {ConversationId} not found.", conversationId);
                return null;
            }

            var conv = conversationSql.Conversation;

            var lastMessagesTask = _messageRepo.GetLastMessageForConversationsAsync(new List<int> { conversationId });

            var unreadCountsTask = _messageRepo.GetUnreadCountsForConversationsAsync(
                 new List<int> { conversationId },
                 userId
             );

            var presenceStatusTask = (conv.ConversationType == EnumConversationType.Direct && conversationSql.PartnerUser != null)
                ? _presenceService.GetUserStatusAsync(conversationSql.PartnerUser.Id)
                : Task.FromResult(EnumUserPresenceStatus.Offline);

            await Task.WhenAll(lastMessagesTask, unreadCountsTask, presenceStatusTask); 

            var lastMessagesDict = await lastMessagesTask;
            var unreadCountsDict = await unreadCountsTask;
            var partnerPresenceStatus = await presenceStatusTask;

            // BƯỚC 3: Kết hợp tất cả dữ liệu lại để tạo DTO
            var dto = new ConversationListItemDTO
            {
                ConversationId = conv.ConversationID,
                GroupId = conv.ExplicitGroupID,
                ConversationType = conv.ConversationType,
                UnreadCount = (int)(unreadCountsDict.TryGetValue(conv.ConversationID, out var count) ? count : 0)
            };

            if (conv.ConversationType == EnumConversationType.Direct)
            {
                if (conversationSql.PartnerUser != null)
                {
                    dto.DisplayName = conversationSql.PartnerUser.FullName;
                    dto.AvatarUrl = conversationSql.PartnerUser.AvatarUrl;
                    dto.PartnerPresenceStatus = partnerPresenceStatus;
                }
            }
            else // Group Chat
            {
                dto.DisplayName = conv.Title;
                dto.AvatarUrl = conv.AvatarUrl;
            }

            if (lastMessagesDict.TryGetValue(conv.ConversationID, out var lastMessage))
            {
                dto.LastMessagePreview = GetMessagePreview(lastMessage);
                dto.LastMessageType = lastMessage.MessageType;
                dto.LastMessageTimestamp = lastMessage.SentAt;
            }
            else
            {
                dto.LastMessagePreview = "Chưa có tin nhắn nào.";
                dto.LastMessageTimestamp = conv.CreatedAt;
            }

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xây dựng ConversationListItemDTO cho conv {ConversationId}, user {UserId}", conversationId, userId);
            return null; // Trả về null nếu có lỗi
        }
    }
}
