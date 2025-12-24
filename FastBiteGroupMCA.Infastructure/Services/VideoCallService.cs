using Amazon.S3.Model;
using FastBiteGroupMCA.Application.DTOs.Hubs;
using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.DTOs.VideoCall;
using FastBiteGroupMCA.Application.Notifications.Templates;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Infastructure.DependencyInjection.Options;
using FastBiteGroupMCA.Infastructure.Hubs;
using Hangfire;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace FastBiteGroupMCA.Infastructure.Services;

public class VideoCallService : IVideoCallService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHubContext<VideoCallHub> _videoCallHubContext;
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly ILogger<VideoCallService> _logger;
    private readonly LiveKitSettings _liveKitSettings;
    private readonly IMessageService _messageService;
    private readonly IUserPresenceService _userPresenceService;
    private readonly INotificationService _notificationService;
    private readonly IOneSignalService _oneSignalService;
    private readonly ILiveKitService _liveKitService;
    private readonly IMessagesRepository _messageRepo;
    private readonly IMapper _mapper;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly RoomServiceClient _livekitRoomService;

    public VideoCallService(
        IUnitOfWork unitOfWork, 
        ICurrentUser currentUser, 
        IHubContext<VideoCallHub> hubContext, 
        ILogger<VideoCallService> logger, 
        IOptions<LiveKitSettings> liveKitSettings, 
        ILiveKitService liveKitService, 
        IMessagesRepository messagesRepository, 
        IMapper mapper, 
        RoomServiceClient livekitRoomService, 
        IBackgroundJobClient backgroundJobClient,
        IHubContext<ChatHub> chatHubContext,
        INotificationService notificationService,
        IMessageService messageService,
        IUserPresenceService userPresenceService,
        IOneSignalService oneSignalService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _videoCallHubContext = hubContext;
        _logger = logger;
        _liveKitSettings = liveKitSettings.Value;
        _liveKitService = liveKitService;
        _messageRepo = messagesRepository;
        _mapper = mapper;
        _backgroundJobClient = backgroundJobClient;
        _livekitRoomService = livekitRoomService;
        _chatHubContext = chatHubContext;
        _notificationService = notificationService;
        _messageService = messageService;
        _userPresenceService = userPresenceService;
        _oneSignalService = oneSignalService;
    }

    public async Task<ApiResponse<StartCallResponseDto>> StartCallAsync(int conversationId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<StartCallResponseDto>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var conversation = await _unitOfWork.Conversations.GetQueryable()
            .Include(c => c.Participants)
            .ThenInclude(p => p.User) 
            .FirstOrDefaultAsync(c => c.ConversationID == conversationId);

        if (conversation == null)
            return ApiResponse<StartCallResponseDto>.Fail("CONVERSATION_NOT_FOUND", "Không tìm thấy cuộc hội thoại.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return ApiResponse<StartCallResponseDto>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        var existingCall = await _unitOfWork.VideoCallSessions.GetQueryable()
            .FirstOrDefaultAsync(s => s.ConversationID == conversationId && s.EndedAt == null);

        if (existingCall != null)
        {
            var joinResponse = await JoinCallGroupAsync(existingCall.VideoCallSessionID); 
            if (!joinResponse.Success) return new ApiResponse<StartCallResponseDto> { Success = false, Errors = joinResponse.Errors, Message = joinResponse.Message };

            return ApiResponse<StartCallResponseDto>.Ok(new StartCallResponseDto
            {
                VideoCallSessionId = existingCall.VideoCallSessionID,
                LivekitToken = joinResponse.Data.LivekitToken,
                LivekitServerUrl = joinResponse.Data.LivekitServerUrl
            });
        }

        if (conversation.ConversationType == EnumConversationType.Group)
        {
            return await CreateGroupCallAsync(conversation, user);
        }
        else // (conversation.ConversationType == EnumConversationType.Direct)
        {
            return await CreateDirectCallAsync(conversation, user);
        }  
    }
    private async Task<ApiResponse<StartCallResponseDto>> CreateDirectCallAsync(Conversation conversation, AppUser caller)
    {
        var receiver = conversation.Participants.FirstOrDefault(p => p.UserID != caller.Id)?.User;
        if (receiver == null)
            return ApiResponse<StartCallResponseDto>.Fail("RECEIVER_NOT_FOUND", "Không tìm thấy người nhận trong cuộc hội thoại.");

        var newSession = new VideoCallSessions
        {
            ConversationID = conversation.ConversationID,
            InitiatorUserID = caller.Id,
            StartedAt = DateTime.UtcNow,
            Status = EnumCallSessionStatus.Ringing
        };

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            
            await _unitOfWork.VideoCallSessions.AddAsync(newSession);
            await _unitOfWork.VideoCallParticipants.AddAsync(new VideoCallParticipants { VideoCallSession = newSession, UserID = caller.Id });
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create direct call session for conversation {ConvId} Step 1", conversation.ConversationID);
            throw;
        }
        try
        {
            var jobId = _backgroundJobClient.Schedule<IVideoCallService>(
            service => service.HandleCallTimeout(newSession.VideoCallSessionID),
            TimeSpan.FromSeconds(20) // Thời gian chờ cuộc gọi
            );

            //await HandleCallTimeout(newSession.VideoCallSessionID);

            // Cập nhật lại session với Job ID
            newSession.TimeoutJobId = jobId;
            _unitOfWork.VideoCallSessions.Update(newSession);
            await _unitOfWork.SaveChangesAsync(); // Lưu lần thứ 2

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule call timeout job for session {SessionId} Step 2", newSession.VideoCallSessionID);
        }

        var callerStatus = await _userPresenceService.GetUserStatusAsync(caller.Id);

        var callerProfileDto = new UserProfileDto
        {
            UserId = caller.Id,
            FullName = caller.FullName!,
            AvatarUrl = caller.AvatarUrl,
            PresenceStatus = callerStatus 
        };

        var callInfo = new IncomingCallDto
        {
            VideoCallSessionId = newSession.VideoCallSessionID,
            ConversationId = conversation.ConversationID,
            Caller = callerProfileDto 
        };

        var receiverStatus = await _userPresenceService.GetUserStatusAsync(receiver.Id);
        if (receiverStatus == EnumUserPresenceStatus.Offline)
        {

            await _oneSignalService.SendNotificationToUserAsync(
                $"{caller.FullName} đang gọi cho bạn...",
                receiver.Id
            );
        }
        else
        {
            await _videoCallHubContext.Clients.User(receiver.Id.ToString())
            .SendAsync("IncomingCall", callInfo);
        }

        var callerToken = _liveKitService.GenerateToken(caller, newSession, null);
        var responseDto = new StartCallResponseDto
        {
            VideoCallSessionId = newSession.VideoCallSessionID,
            LivekitToken = callerToken,
            LivekitServerUrl = _liveKitSettings.Url
        };

        return ApiResponse<StartCallResponseDto>.Ok(responseDto);
    }

    private async Task<ApiResponse<StartCallResponseDto>> CreateGroupCallAsync(Conversation conversation, AppUser user)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var membership = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == conversation.ExplicitGroupID && gm.UserID == user.Id);
            if (membership == null)
                return ApiResponse<StartCallResponseDto>.Fail("FORBIDDEN", "Bạn không phải thành viên của nhóm này.");

            var newSession = new VideoCallSessions
            {
                ConversationID = conversation.ConversationID,
                InitiatorUserID = user.Id,
                StartedAt = DateTime.UtcNow,
                Status = EnumCallSessionStatus.Ongoing
            };
            await _unitOfWork.VideoCallSessions.AddAsync(newSession);
            await _unitOfWork.VideoCallParticipants.AddAsync(new VideoCallParticipants { VideoCallSession = newSession, UserID = user.Id });
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            var token = _liveKitService.GenerateToken(user, newSession, membership.Role);
            await CreateAndBroadcastCallNotificationAsync(conversation.ConversationID, newSession, user);

            return ApiResponse<StartCallResponseDto>.Ok(new StartCallResponseDto
            {
                VideoCallSessionId = newSession.VideoCallSessionID,
                LivekitToken = token,
                LivekitServerUrl = _liveKitSettings.Url
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to start group call for conversation {ConvId}", conversation.ConversationID);
            throw;
        }
    }

    private async Task CreateAndBroadcastCallNotificationAsync(int conversationId, VideoCallSessions session, AppUser user)
    {
        try
        {
            var callMessage = new Messages
            {
                ConversationId = conversationId,
                MessageType = EnumMessageType.VideoCall,
                SentAt = session.StartedAt,
                Sender = new SenderInfo { UserId = user.Id, DisplayName = user.FullName!, AvatarUrl = user.AvatarUrl },
                Content = System.Text.Json.JsonSerializer.Serialize(new
                {
                    videoCallSessionId = session.VideoCallSessionID.ToString()
                })
            };
            await _messageRepo.InsertOneAsync(callMessage);

            // === BƯỚC 2: TỐI ƯU - LẤY DỮ LIỆU HÀNG LOẠT ===
            var conversation = await _unitOfWork.Conversations.GetByIdAsync(conversationId);
            if (conversation == null) return;

            var allParticipantUsers = await _unitOfWork.ConversationParticipants.GetQueryable()
                .Where(p => p.ConversationID == conversationId)
                .Select(p => p.User)
                .ToListAsync();

            var participantRoles = new Dictionary<Guid, EnumGroupRole>();
            if (conversation.ConversationType == EnumConversationType.Group && conversation.ExplicitGroupID.HasValue)
            {
                participantRoles = await _unitOfWork.GroupMembers.GetQueryable()
                    .AsNoTracking()
                    .Where(gm => gm.GroupID == conversation.ExplicitGroupID.Value)
                    .ToDictionaryAsync(gm => gm.UserID, gm => gm.Role);
            }

            foreach (var participant in allParticipantUsers)
            {
                if (participant == null) continue;

                var messageDto = _mapper.Map<MessageDTO>(callMessage);

                participantRoles.TryGetValue(participant.Id, out var participantRoleInGroup);

                messageDto.IsMine = (participant.Id == user.Id);
                messageDto.CanEdit = false; 
                messageDto.CanDelete = (participant.Id == user.Id) || (participantRoleInGroup > EnumGroupRole.Member);

                if (conversation.ConversationType == EnumConversationType.Group)
                {
                    participantRoles.TryGetValue(user.Id, out var senderRole);
                    messageDto.SenderRoleInGroup = senderRole;
                }

                await _chatHubContext.Clients.User(participant.Id.ToString())
                    .SendAsync("ReceiveMessage", messageDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send call notification message for session {SessionId}", session.VideoCallSessionID);
        }
    }

    public async Task<ApiResponse<JoinCallResponseDto>> JoinCallGroupAsync(Guid videoCallSessionId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<JoinCallResponseDto>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ.");

        var validationData = await _unitOfWork.VideoCallSessions.GetQueryable()
            .Where(s => s.VideoCallSessionID == videoCallSessionId)
            .Select(s => new
            {
                Session = s,
                UserMembership = s.Conversation.Group.Members.FirstOrDefault(m => m.UserID == userId),
                IsAlreadyParticipant = s.Participants.Any(p => p.UserID == userId)
            })
            .FirstOrDefaultAsync();

        if (validationData == null)
            return ApiResponse<JoinCallResponseDto>.Fail("CALL_NOT_FOUND", "Không tìm thấy cuộc gọi.");

        var session = validationData.Session;
        if (session.EndedAt.HasValue)
            return ApiResponse<JoinCallResponseDto>.Fail("CALL_ENDED", "Cuộc gọi này đã kết thúc.");

        if (validationData.UserMembership == null)
            return ApiResponse<JoinCallResponseDto>.Fail("FORBIDDEN", "Bạn không có quyền tham gia cuộc gọi trong nhóm này.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return ApiResponse<JoinCallResponseDto>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        if (!validationData.IsAlreadyParticipant)
        {
            var participant = new VideoCallParticipants
            {
                VideoCallSessionID = videoCallSessionId,
                UserID = userId,
                JoinedAt = DateTime.UtcNow
            };
            await _unitOfWork.VideoCallParticipants.AddAsync(participant);
            await _unitOfWork.SaveChangesAsync();

            var userProfileDto = _mapper.Map<UserProfileDto>(user);
            _backgroundJobClient.Enqueue(() =>
                BroadcastParticipantJoinedAsync(videoCallSessionId, userProfileDto));
        }

        var userRole = validationData.UserMembership.Role;
        var token = _liveKitService.GenerateToken(user, session, userRole);

        var responseDto = new JoinCallResponseDto
        {
            LivekitToken = token,
            LivekitServerUrl = _liveKitSettings.Url
        };

        return ApiResponse<JoinCallResponseDto>.Ok(responseDto, "Tham gia cuộc gọi thành công.");
    }

    [DisplayName("Broadcast Participant Joined for Session: {0}")]
    public async Task BroadcastParticipantJoinedAsync(Guid sessionId, UserProfileDto joiningUser)
    {
        await _videoCallHubContext.Clients
            .Group($"call_{sessionId}") 
            .SendAsync("ParticipantJoined", joiningUser);
    }

    public async Task<ApiResponse<object>> LeaveCallGroupAsync(Guid videoCallSessionId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var participantRecord = await _unitOfWork.VideoCallParticipants.GetQueryable()
                .FirstOrDefaultAsync(p => p.VideoCallSessionID == videoCallSessionId &&
                                          p.UserID == userId &&
                                          p.LeftAt == null); 

            if (participantRecord == null)
            {
                return ApiResponse<object>.Ok(null, "Hành động đã được ghi nhận.");
            }

            participantRecord.LeftAt = DateTime.UtcNow;
            _unitOfWork.VideoCallParticipants.Update(participantRecord);
            await _unitOfWork.SaveChangesAsync();

            var remainingActiveParticipants = await _unitOfWork.VideoCallParticipants.GetQueryable()
                .CountAsync(p => p.VideoCallSessionID == videoCallSessionId && p.LeftAt == null);

            if (remainingActiveParticipants == 0)
            {
                var session = await _unitOfWork.VideoCallSessions.GetByIdAsync(videoCallSessionId);
                if (session != null)
                {
                    session.EndedAt = DateTime.UtcNow;
                    _unitOfWork.VideoCallSessions.Update(session);
                    await _unitOfWork.SaveChangesAsync(); 
                }
            }

            await transaction.CommitAsync();

            return ApiResponse<object>.Ok(null, "Ghi nhận rời khỏi cuộc gọi thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error while processing leave call for user {UserId} in session {SessionId}", userId, videoCallSessionId);
            throw;
        }
    }

    public async Task<ApiResponse<PagedResult<CallHistoryItemDTO>>> GetCallHistoryAsync(int conversationId, GetCallHistoryQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PagedResult<CallHistoryItemDTO>>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        try
        {
            var historyQuery = _unitOfWork.VideoCallSessions.GetQueryable()
                .AsNoTracking()
                .Where(s => s.ConversationID == conversationId && s.EndedAt != null)
                .Join(
                    _unitOfWork.Users.GetQueryable(),
                    session => session.InitiatorUserID,
                    user => user.Id,
                    (session, initiatorUser) => new { Session = session, Initiator = initiatorUser }
                )
                .OrderByDescending(x => x.Session.StartedAt)
                .Select(x => new CallHistoryItemDTO
                {
                    VideoCallSessionId = x.Session.VideoCallSessionID,
                    InitiatorUserId = x.Session.InitiatorUserID,
                    InitiatorName = x.Initiator.FullName ?? "[Không rõ]",
                    StartedAt = x.Session.StartedAt,
                    EndedAt = x.Session.EndedAt,
                    ParticipantCount = x.Session.Participants.Count()
                });

            var pagedResult = await historyQuery.ToPagedResultAsync(query.PageNumber, query.PageSize);

            return ApiResponse<PagedResult<CallHistoryItemDTO>>.Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy lịch sử cuộc gọi cho cuộc trò chuyện {ConversationId}", conversationId);
            return ApiResponse<PagedResult<CallHistoryItemDTO>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    private async Task<bool> IsUserHostOfCall(Guid userId, Guid videoCallSessionId)
    {
        var session = await _unitOfWork.VideoCallSessions.GetByIdAsync(videoCallSessionId);
        if (session == null) return false;

        if (userId == session.InitiatorUserID) return true;

        var conversation = await _unitOfWork.Conversations.GetByIdAsync(session.ConversationID);

        if (conversation?.ConversationType != EnumConversationType.Group || !conversation.ExplicitGroupID.HasValue)
        {
            return false;
        }

        var groupId = conversation.ExplicitGroupID.Value;

        var membership = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);

        return membership?.Role == EnumGroupRole.Admin || membership?.Role == EnumGroupRole.Moderator;
    }

    public async Task<ApiResponse<object>> MuteParticipantTrackAsync(Guid videoCallSessionId, Guid targetUserId, TrackSource source)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        if (!await IsUserHostOfCall(currentUserId, videoCallSessionId))
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền thực hiện hành động này.");

        var participants = await _livekitRoomService.ListParticipants(new ListParticipantsRequest 
        { 
            Room = videoCallSessionId.ToString() 
        });
        
        var targetParticipant = participants.Participants.FirstOrDefault(p => p.Identity == targetUserId.ToString());
        var targetTrack = targetParticipant?.Tracks.FirstOrDefault(t => t.Source == source);

        if (targetTrack == null)
        {
            return ApiResponse<object>.Fail("TRACK_NOT_FOUND", "Không tìm thấy luồng media tương ứng để tắt.");
        }

        var muteRequest = new MuteRoomTrackRequest
        {
            Room = videoCallSessionId.ToString(),
            Identity = targetUserId.ToString(),
            TrackSid = targetTrack.Sid, 
            Muted = true
        };
        await _livekitRoomService.MutePublishedTrack(muteRequest);

        return ApiResponse<object>.Ok(null, "Hành động đã được thực hiện.");
    }

    public async Task<ApiResponse<object>> RemoveParticipantAsync(Guid videoCallSessionId, Guid targetUserId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        if (!await IsUserHostOfCall(currentUserId, videoCallSessionId))
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền thực hiện hành động này.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var request = new RoomParticipantIdentity
            {
                Room = videoCallSessionId.ToString(),
                Identity = targetUserId.ToString()
            };
            await _livekitRoomService.RemoveParticipant(request);

            var participantRecord = await _unitOfWork.VideoCallParticipants.GetQueryable()
                .FirstOrDefaultAsync(p => p.VideoCallSessionID == videoCallSessionId && p.UserID == targetUserId);

            if (participantRecord != null)
            {
                _unitOfWork.VideoCallParticipants.Remove(participantRecord);
                await _unitOfWork.SaveChangesAsync();
            }

            await transaction.CommitAsync();


            return ApiResponse<object>.Ok(null, "Đã xóa người tham gia khỏi cuộc gọi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi xóa người tham gia {TargetUserId} khỏi cuộc gọi {SessionId}", targetUserId, videoCallSessionId);
            return ApiResponse<object>.Fail("SERVER_ERROR", "Có lỗi xảy ra.");
        }
    }

    public async Task<ApiResponse<object>> EndCallForAllAsync(Guid videoCallSessionId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        if (!await IsUserHostOfCall(currentUserId, videoCallSessionId))
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền kết thúc cuộc gọi này.");

        try
        {
            var request = new DeleteRoomRequest { Room = videoCallSessionId.ToString() };
            await _livekitRoomService.DeleteRoom(request);
            _logger.LogInformation("Successfully deleted LiveKit room {SessionId}", videoCallSessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete LiveKit room {SessionId}. Aborting operation.", videoCallSessionId);
            return ApiResponse<object>.Fail("LIVEKIT_ERROR", "Không thể kết thúc phiên gọi trên máy chủ media.");
        }

        var session = await _unitOfWork.VideoCallSessions.GetByIdAsync(videoCallSessionId);
        if (session != null && !session.EndedAt.HasValue)
        {
            session.EndedAt = DateTime.UtcNow;
            session.Status = EnumCallSessionStatus.Ended;
            await _unitOfWork.SaveChangesAsync();

            // Gửi tin nhắn hệ thống
            var duration = session.EndedAt.Value - session.StartedAt;
            var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            await _messageService.SendSystemMessageAsync(
                session.ConversationID,
                $"Cuộc gọi đã được kết thúc bởi {currentUser?.FullName}. Thời lượng: {duration:mm' phút 'ss' giây'}"
            );
        }

        return ApiResponse<object>.Ok(null, "Cuộc gọi đã được kết thúc cho tất cả mọi người.");
    }

    public async Task<ApiResponse<AcceptCallResponseDTO>> AcceptDirectCallAsync(Guid videoCallSessionId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var receiverId))
            return ApiResponse<AcceptCallResponseDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var session = await _unitOfWork.VideoCallSessions.GetQueryable()
            .Include(s => s.Conversation!.Participants) 
            .FirstOrDefaultAsync(s => s.VideoCallSessionID == videoCallSessionId);

        if (session == null || session.EndedAt.HasValue)
            return ApiResponse<AcceptCallResponseDTO>.Fail("CALL_NOT_FOUND", "Cuộc gọi không tồn tại hoặc đã kết thúc.");

        bool isReceiver = session.Conversation!.Participants.Any(p => p.UserID == receiverId && p.UserID != session.InitiatorUserID);
        if (!isReceiver)
            return ApiResponse<AcceptCallResponseDTO>.Fail("FORBIDDEN", "Bạn không phải là người nhận của cuộc gọi này.", 403);


        var updatedRows = await _unitOfWork.VideoCallSessions.GetQueryable()
            .Where(s => s.VideoCallSessionID == videoCallSessionId && s.Status == EnumCallSessionStatus.Ringing)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(s => s.Status, EnumCallSessionStatus.Ongoing)
            );

        if (updatedRows == 0)
        {
            return ApiResponse<AcceptCallResponseDTO>.Fail("CALL_UNAVAILABLE", "Cuộc gọi này không còn hiệu lực (có thể đã bị nhỡ).");
        }


        await _unitOfWork.VideoCallParticipants.AddAsync(new VideoCallParticipants { VideoCallSessionID = videoCallSessionId, UserID = receiverId });
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrEmpty(session.TimeoutJobId))
        {
            _backgroundJobClient.Delete(session.TimeoutJobId);
        }

        var receiver = await _unitOfWork.Users.GetByIdAsync(receiverId);
        var receiverToken = _liveKitService.GenerateToken(receiver, session, null);

        await _videoCallHubContext.Clients.User(session.InitiatorUserID.ToString()).SendAsync("CallAccepted", videoCallSessionId, _mapper.Map<UserProfileDto>(receiver));

        var responseDto = new AcceptCallResponseDTO
        {
            LivekitToken = receiverToken,
            LivekitServerUrl = _liveKitSettings.Url
        };
        return ApiResponse<AcceptCallResponseDTO>.Ok(responseDto);
    }

    [NonAction]
    public async Task HandleCallTimeout(Guid videoCallSessionId)
    {
        var session = await _unitOfWork.VideoCallSessions.GetQueryable()
            .Include(s => s.Conversation)
            .FirstOrDefaultAsync(s => s.VideoCallSessionID == videoCallSessionId);

        if (session != null && session.Status == EnumCallSessionStatus.Ringing)
        {
            session.Status = EnumCallSessionStatus.Missed;
            session.EndedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            await _videoCallHubContext.Clients.User(session.InitiatorUserID.ToString())
                .SendAsync("CallMissed", videoCallSessionId);

            var initiatorUser = await _unitOfWork.Users.GetByIdAsync(session.InitiatorUserID);
            var receiverUser = await _unitOfWork.ConversationParticipants.GetQueryable()
                .Where(p => p.ConversationID == session.ConversationID && p.UserID != session.InitiatorUserID)
                .Select(p => p.User)
                .FirstOrDefaultAsync();

            if (receiverUser != null && initiatorUser != null)
            {
                var eventData = new MissedCallEventData(initiatorUser, session.Conversation!);

                await _notificationService.DispatchNotificationAsync<MissedCallNotificationTemplate, MissedCallEventData>(
                    receiverUser.Id,
                    eventData);
            }

            await _messageService.SendSystemMessageAsync(
                session.ConversationID,
                $"Cuộc gọi nhỡ lúc {session.StartedAt.ToLocalTime():HH:mm}"
            );
        }
    }

    public async Task<ApiResponse<object>> DeclineDirectCallAsync(Guid sessionId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var session = await _unitOfWork.VideoCallSessions.GetByIdAsync(sessionId);
        if (session == null || session.Status != EnumCallSessionStatus.Ringing)
            return ApiResponse<object>.Fail("CALL_UNAVAILABLE", "Cuộc gọi không còn hiệu lực để từ chối.");

        var isReceiver = await _unitOfWork.ConversationParticipants.GetQueryable()
            .AnyAsync(p => p.ConversationID == session.ConversationID && p.UserID == userId && p.UserID != session.InitiatorUserID);
        if (!isReceiver)
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền từ chối cuộc gọi này.", 403);

        session.Status = EnumCallSessionStatus.Declined;
        session.EndedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        await _videoCallHubContext.Clients.User(session.InitiatorUserID.ToString())
            .SendAsync("CallDeclined", sessionId);

        return ApiResponse<object>.Ok(null, "Đã từ chối cuộc gọi.");
    }

    public async Task<ApiResponse<object>> LeaveCallAsync(Guid sessionId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        // Dùng Include để lấy các thông tin cần thiết trong 1 lần gọi
        var session = await _unitOfWork.VideoCallSessions.GetQueryable()
            .Include(s => s.Conversation)
            .Include(s => s.Participants) // Lấy danh sách người đang tham gia phiên gọi
            .FirstOrDefaultAsync(s => s.VideoCallSessionID == sessionId);

        if (session == null || session.EndedAt.HasValue)
            return ApiResponse<object>.Fail("CALL_NOT_FOUND", "Cuộc gọi không tồn tại hoặc đã kết thúc.");

        var participantRecord = session.Participants.FirstOrDefault(p => p.UserID == userId);
        if (participantRecord == null)
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không thuộc cuộc gọi này.", 403);

        try
        {
            await _livekitRoomService.RemoveParticipant(new RoomParticipantIdentity
            {
                Room = sessionId.ToString(),
                Identity = userId.ToString()
            });
            _logger.LogInformation("Successfully removed participant {UserId} from LiveKit room {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove participant {UserId} from LiveKit room {SessionId}. Proceeding with DB update.", userId, sessionId);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _unitOfWork.VideoCallParticipants.Remove(participantRecord);
            await _unitOfWork.SaveChangesAsync();

            var remainingParticipants = session.Participants.Count();

            if (session.Conversation.ConversationType == EnumConversationType.Group)
            {
                if (remainingParticipants == 0)
                {
                    session.Status = EnumCallSessionStatus.Ended;
                    session.EndedAt = DateTime.UtcNow;
                    _logger.LogInformation("Last participant left. Ending group call {SessionId}", sessionId);
                    var duration = DateTime.UtcNow - session.StartedAt;
                    await _messageService.SendSystemMessageAsync(session.ConversationID, $"Cuộc gọi nhóm đã kết thúc. Thời lượng: {duration:mm\\:ss}");
                }
                else
                {
                    await _videoCallHubContext.Clients.Group($"call_{sessionId}").SendAsync("ParticipantLeft", userId);
                    _logger.LogInformation("Participant {UserId} left group call {SessionId}", userId, sessionId);
                }
            }
            else // EnumConversationType.Direct
            {
                // === LOGIC CHO CUỘC GỌI 1-1 ===
                session.Status = EnumCallSessionStatus.Ended;
                session.EndedAt = DateTime.UtcNow;

                var otherParticipant = await _unitOfWork.ConversationParticipants.GetQueryable()
                    .FirstOrDefaultAsync(p => p.ConversationID == session.ConversationID && p.UserID != userId);

                if (otherParticipant != null)
                {
                    await _videoCallHubContext.Clients.User(otherParticipant.UserID.ToString()).SendAsync("CallEnded", sessionId);
                }

                var duration = DateTime.UtcNow - session.StartedAt;
                await _messageService.SendSystemMessageAsync(session.ConversationID, $"Cuộc gọi đã kết thúc. Thời lượng: {duration:mm\\:ss}");
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<object>.Ok(null, "Đã rời khỏi cuộc gọi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi rời khỏi cuộc gọi {SessionId} cho người dùng {UserId}", sessionId, userId);
            return ApiResponse<object>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }
}
