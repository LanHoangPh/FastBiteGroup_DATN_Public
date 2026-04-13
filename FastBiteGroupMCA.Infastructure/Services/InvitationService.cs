using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.Invitation;
using FastBiteGroupMCA.Application.Notifications.Templates;

namespace FastBiteGroupMCA.Infastructure.Services;

public class InvitationService : IInvitationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IMessageService _messageService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(
        IUnitOfWork unitOfWork, 
        ICurrentUser currentUser,
        INotificationService notificationService,
        IMessageService messageService,
        ILogger<InvitationService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<ApiResponse<JoinGroupResponseDTO>> AcceptInviteLinkAsync(string invitationCode)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userGuid))
            return ApiResponse<JoinGroupResponseDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var validationResult = await ValidateInviteLink(invitationCode);
            if (validationResult.link == null)
                return ApiResponse<JoinGroupResponseDTO>.Fail(validationResult.errorCode!, validationResult.errorMessage!);

            var invitation = validationResult.link;

            if (await _unitOfWork.GroupMembers.GetQueryable()
                .AnyAsync(gm => gm.GroupID == invitation.GroupID && gm.UserID == userGuid))
                return ApiResponse<JoinGroupResponseDTO>.Fail("AlreadyMember", "Bạn đã là thành viên của nhóm này.");

            invitation.CurrentUses++;

            _unitOfWork.GroupInvitations.Update(invitation);

            await AddUserToGroupAndConversationAsync(invitation.GroupID, userGuid);

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            var conversation = await _unitOfWork.Conversations.GetQueryable()
                .AsNoTracking().FirstOrDefaultAsync(c => c.ExplicitGroupID == invitation.GroupID);

            var responseDto = new JoinGroupResponseDTO { GroupId = invitation.GroupID, DefaultConversationId = conversation?.ConversationID ?? 0 };
            return ApiResponse<JoinGroupResponseDTO>.Ok(responseDto, "Tham gia nhóm thành công!");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency conflict when accepting invite code {Code}", invitationCode);
            return ApiResponse<JoinGroupResponseDTO>.Fail("Conflict", "Lời mời này vừa được người khác sử dụng. Vui lòng thử lại.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error accepting invite code {Code}", invitationCode);
            return ApiResponse<JoinGroupResponseDTO>.Fail("ServerError", "Có lỗi xảy ra, vui lòng thử lại.");
        }
    }
    private async Task<(GroupInvitations? link, string? errorCode, string? errorMessage)> ValidateInviteLink(string code)
    {
        var invitation = await _unitOfWork.GroupInvitations.GetQueryable()
            .FirstOrDefaultAsync(i => i.InvitationCode == code && i.IsActive);

        if (invitation == null)
            return (null, "InvalidLink", "Link mời không hợp lệ hoặc đã bị vô hiệu hóa.");

        if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
            return (null, "ExpiredLink", "Link mời đã hết hạn.");

        if (invitation.MaxUses.HasValue && invitation.CurrentUses >= invitation.MaxUses.Value)
            return (null, "UsageLimitExceeded", "Link mời đã hết lượt sử dụng.");

        return (invitation, null, null);
    }

    private async Task AddUserToGroupAndConversationAsync(Guid groupId, Guid userId)
    {
        await _unitOfWork.GroupMembers.AddAsync(new GroupMember { GroupID = groupId, UserID = userId, Role = EnumGroupRole.Member, JoinedAt = DateTime.UtcNow });
        var conversation = await _unitOfWork.Conversations.GetQueryable().FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);
        if (conversation != null)
        {
            await _unitOfWork.ConversationParticipants.AddAsync(new ConversationParticipants { ConversationID = conversation.ConversationID, UserID = userId, JoinedAt = DateTime.UtcNow });
        }
    }

    public async Task<ApiResponse<GroupPreviewDTO>> GetGroupPreviewByCodeAsync(string invitationCode)
    {
        var validationResult = await ValidateInviteLink(invitationCode);
        if (validationResult.link == null)
        {
            return ApiResponse<GroupPreviewDTO>.Fail("INVITATION_CODE_INVALID", validationResult.errorMessage!);
        }

        var groupPreview = await _unitOfWork.Groups.GetQueryable()
            .Where(g => g.GroupID == validationResult.link.GroupID && !g.IsDeleted)
            .Select(g => new GroupPreviewDTO
            {
                GroupId = g.GroupID,
                GroupName = g.GroupName,
                GroupAvatarUrl = g.GroupAvatarUrl,
                MemberCount = g.Members.Count()
            })
            .FirstOrDefaultAsync();

        if (groupPreview == null)
        {
            return ApiResponse<GroupPreviewDTO>.Fail("GROUP_NOT_FOUND", "Nhóm liên kết với lời mời không còn tồn tại.");
        }

        return ApiResponse<GroupPreviewDTO>.Ok(groupPreview);
    }

    public async Task<ApiResponse<List<GroupInvitationDTO>>> GetPendingInvitationsAsync()
    {
        if (!Guid.TryParse(_currentUser.Id, out var userID))
        {
            return ApiResponse<List<GroupInvitationDTO>>.Fail("Unauthorized", "Người dùng không hợp lệ.", 401);
        }
        var invitations = await _unitOfWork.UserGroupInvitations
            .GetQueryable()
            .Include(i => i.Group)
            .Include(i => i.InvitedByUser)
            .Where(i => i.InvitedUserID == userID && i.Status == EnumInvitationStatus.Pending && i.Group != null && i.InvitedByUser != null)
            .Select(i => new GroupInvitationDTO
            {
                InvitationId = i.InvitationID,
                GroupName = i.Group!.GroupName,
                GroupAvatarUrl = i.Group.GroupAvatarUrl,
                InvitedByName = i.InvitedByUser!.FullName ?? "Một người dùng" 
            })
            .ToListAsync();

        return ApiResponse<List<GroupInvitationDTO>>.Ok(invitations, "Lấy danh sách lời mời thành công.");
    }

    public async Task<ApiResponse<object>> RespondToInvitationAsync(int invitationId, RespondToInvitationDTO dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userGuid))
            return ApiResponse<object>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        var invitation = await _unitOfWork.UserGroupInvitations
                   .GetQueryable()
                   .Include(i => i.Group)
                   .Include(i => i.InvitedByUser)
                   .FirstOrDefaultAsync(i => i.InvitationID == invitationId && i.InvitedUserID == userGuid);

        var conversation = await _unitOfWork.Conversations
                .GetQueryable()
                .FirstOrDefaultAsync(c => c.ExplicitGroupID == invitation.GroupID);

        if (invitation == null || invitation.Status != EnumInvitationStatus.Pending)
        {
            return ApiResponse<object>.Fail("INVALID_INVITATION", "Lời mời không hợp lệ hoặc đã được xử lý.", 404);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (dto.Accept)
            {
                invitation.Status = EnumInvitationStatus.Accepted;

                await _unitOfWork.GroupMembers.AddAsync(new GroupMember
                {
                    GroupID = invitation.GroupID,
                    UserID = userGuid,
                    Role = EnumGroupRole.Member,
                    JoinedAt = DateTime.UtcNow
                });

                if (conversation != null)
                {
                    await _unitOfWork.ConversationParticipants.AddAsync(new ConversationParticipants
                    {
                        ConversationID = conversation.ConversationID,
                        UserID = userGuid,
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }
            else 
            {
                invitation.Status = EnumInvitationStatus.Declined;
            }
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        catch (Exception ex) 
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi xử lý lời mời {InvitationId} cho user {UserId}", invitationId, userGuid);
            return ApiResponse<object>.Fail("ServerError", "Đã có lỗi xảy ra trong quá trình xử lý.", 500);
        }

        if (dto.Accept)
        {
            var acceptedUser = await _unitOfWork.Users.GetByIdAsync(userGuid); 
            if (acceptedUser != null && invitation.Group != null)
            {
                if (conversation != null)
                {
                    var systemMessageContent = $"{acceptedUser.FullName} đã tham gia nhóm.";
                    try { await _messageService.SendSystemMessageAsync(conversation.ConversationID, systemMessageContent); }
                    catch (Exception msgEx) { _logger.LogError(msgEx, "Lỗi gửi tin nhắn hệ thống khi user {UserId} tham gia nhóm {GroupId}", userGuid, invitation.GroupID); }
                }

                var inviter = invitation.InvitedByUser; 
                if (inviter != null)
                {
                    var eventData = new InvitationAcceptedEventData(acceptedUser, invitation.Group);
                    try
                    {
                        await _notificationService.DispatchNotificationAsync<InvitationAcceptedNotificationTemplate, InvitationAcceptedEventData>(
                            inviter.Id,
                            eventData
                        );
                    }
                    catch (Exception notifyEx) { _logger.LogError(notifyEx, "Lỗi gửi thông báo chấp nhận lời mời cho user {InviterId}", inviter.Id); }
                }
            }
        }
            return ApiResponse<object>.Ok(null!, "Phản hồi lời mời thành công.");
    }

    public async Task<ApiResponse<PagedResult<SentGroupInvitationDTO>>> GetSentInvitationsByGroupAsync(Guid groupId, GetSentInvitationsQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<PagedResult<SentGroupInvitationDTO>>.Fail("Unauthorized", "Người dùng không hợp lệ.", 401);

        var userMembership = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == currentUserId);

        if (userMembership == null || userMembership.Role == EnumGroupRole.Member)
        {
            return ApiResponse<PagedResult<SentGroupInvitationDTO>>.Fail("Forbidden", "Bạn không có quyền xem danh sách lời mời của nhóm này.", 403);
        }

        try
        {
            var queryable = _unitOfWork.UserGroupInvitations.GetQueryable()
                .Include(i => i.InvitedUser)  
                .Include(i => i.InvitedByUser) 
                .Where(i => i.GroupID == groupId);

            if (query.Status.HasValue)
            {
                queryable = queryable.Where(i => i.Status == query.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.Trim();
                queryable = queryable.Where(i => i.InvitedUser != null && i.InvitedUser.FullName!.Contains(searchTerm));
            }

            var pagedResult = await queryable
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new SentGroupInvitationDTO
                {
                    InvitationId = i.InvitationID,
                    Status = i.Status,
                    InvitedAt = i.CreatedAt,
                    InvitedUserId = i.InvitedUserID,
                    InvitedUserFullName = i.InvitedUser!.FullName ?? "N/A",
                    InvitedUserAvatarUrl = i.InvitedUser.AvatarUrl,
                    InvitedByUserId = i.InvitedByUserID,
                    InvitedByFullName = i.InvitedByUser!.FullName ?? "N/A"
                })
                .ToPagedResultAsync(query.PageNumber, query.PageSize);

            return ApiResponse<PagedResult<SentGroupInvitationDTO>>.Ok(pagedResult, "Lấy danh sách lời mời đã gửi thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách lời mời đã gửi cho nhóm {GroupId}", groupId);
            return ApiResponse<PagedResult<SentGroupInvitationDTO>>.Fail("ServerError", "Đã có lỗi hệ thống xảy ra.", 500);
        }
    }

    public async Task<ApiResponse<object>> RevokeInvitationAsync(Guid groupId, int invitationId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<object>.Fail("Unauthorized", "Người dùng không hợp lệ.", 401);

        var invitation = await _unitOfWork.UserGroupInvitations.GetQueryable()
            .FirstOrDefaultAsync(i => i.InvitationID == invitationId && i.GroupID == groupId);

        if (invitation == null)
        {
            return ApiResponse<object>.Fail("INVITATION_NOT_FOUND", "Không tìm thấy lời mời này trong nhóm.", 404);
        }

        var userMembership = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == currentUserId);

        bool isOwnerOrAdmin = userMembership != null && userMembership.Role != EnumGroupRole.Member;
        bool isTheOneWhoInvited = invitation.InvitedByUserID == currentUserId;

        if (!isOwnerOrAdmin && !isTheOneWhoInvited)
        {
            return ApiResponse<object>.Fail("Forbidden", "Bạn không có quyền thu hồi lời mời này.", 403);
        }
        if (invitation.Status != EnumInvitationStatus.Pending)
        {
            return ApiResponse<object>.Fail("INVALID_STATUS", $"Chỉ có thể thu hồi lời mời đang ở trạng thái 'Pending'. Trạng thái hiện tại: {invitation.Status}.", 400);
        }

        _unitOfWork.UserGroupInvitations.Remove(invitation);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<object>.Ok(null, "Thu hồi lời mời thành công.");
    }
}
