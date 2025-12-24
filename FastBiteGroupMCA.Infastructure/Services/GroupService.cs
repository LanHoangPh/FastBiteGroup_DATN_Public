using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.Invitation;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices.BackgroundJob;
using FastBiteGroupMCA.Application.IServices.FileStorage;
using FastBiteGroupMCA.Application.Notifications.Templates;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Hangfire;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FastBiteGroupMCA.Infastructure.Services
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<GroupService> _logger;
        private readonly IFileService _fileService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IUserPresenceService _presenceService;
        private readonly IMessageService _messageService;
        private readonly INotificationService _notificationService;
        private readonly StorageStrategy _storageStrategy;
        private readonly IAdminNotificationService _adminNotificationService;
        public GroupService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUser currentUser,
            ILogger<GroupService> logger,
            INotificationService notificationService,
            StorageStrategy storageStrategy,
            IAdminNotificationService adminNotificationService,
            IFileService fileService,
            IMessageService messageService,
            IUserPresenceService presenceService,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService;
            _storageStrategy = storageStrategy ?? throw new ArgumentNullException(nameof(storageStrategy));
            _adminNotificationService = adminNotificationService;
            _fileService = fileService;
            _messageService = messageService;
            _presenceService = presenceService;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<ApiResponse<PagedResult<UserGroupDTO>>> GetUserAssociatedGroupsAsync(GetUserGroupsQuery query)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                return ApiResponse<PagedResult<UserGroupDTO>>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ.");
            }

            try
            {
                var baseQuery = _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => gm.UserID == userId && !gm.Group!.IsDeleted);

                var filteredQuery = query.FilterType switch
                {
                    MyGroupFilterType.Chat => baseQuery.Where(gm => gm.Group!.GroupType == EnumGroupType.Public || gm.Group!.GroupType == EnumGroupType.Private),
                    MyGroupFilterType.Community => baseQuery.Where(gm => gm.Group!.GroupType == EnumGroupType.Community),
                    _ => baseQuery
                };

                if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                {
                    var searchTermTrimmed = query.SearchTerm.Trim();
                    filteredQuery = filteredQuery.Where(gm => gm.Group!.GroupName.StartsWith(searchTermTrimmed));
                }

                var pagedResult = await filteredQuery
                    .Select(gm => new
                    {
                        Group = gm.Group, 
                        UserRoleInGroup = gm.Role,
                        ConversationId = _unitOfWork.Conversations.GetQueryable()
                                             .Where(c => c.ExplicitGroupID == gm.GroupID)
                                             .Select(c => (int?)c.ConversationID)
                                             .FirstOrDefault()
                    })
                    .OrderBy(x => x.Group!.GroupName)
                    .Select(x => new UserGroupDTO
                    {
                        GroupId = x.Group!.GroupID,
                        GroupName = x.Group!.GroupName,
                        Description = x.Group!.Description,
                        AvatarUrl = x.Group!.GroupAvatarUrl,
                        GroupType = (x.Group!.GroupType == EnumGroupType.Community
                            ? GroupTypeApiDto.Community
                            : GroupTypeApiDto.Chat),
                        Privacy = x.Group!.Privacy,
                        ConversationId = x.ConversationId ?? 0,

                        MemberCount = x.Group.Members.Count(),
                        IsOwner = x.Group.CreatedByUserID == userId, 
                        IsAdmin = x.UserRoleInGroup == EnumGroupRole.Admin || x.Group.CreatedByUserID == userId // Admin hoặc Owner đều có quyền Admin
                    })
                    .ToPagedResultAsync(query.PageNumber, query.PageSize);

                return ApiResponse<PagedResult<UserGroupDTO>>.Ok(pagedResult, "Lấy danh sách nhóm thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting associated groups for user {UserId}", userId);
                return ApiResponse<PagedResult<UserGroupDTO>>.Fail("GET_GROUPS_FAILED", "Có lỗi xảy ra khi lấy danh sách nhóm của bạn.");
            }
        }
        
        public async Task<ApiResponse<PagedResult<GroupMemberListDto>>> GetGroupMembersAsync(Guid groupId, GetGroupMembersQuery query)
        {
            if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
                return ApiResponse<PagedResult<GroupMemberListDto>>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ.");

            var currentUserMembership = await _unitOfWork.GroupMembers.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == currentUserId);

            if (currentUserMembership == null)
                return ApiResponse<PagedResult<GroupMemberListDto>>.Fail("FORBIDDEN", "Bạn không phải thành viên của nhóm này.", 403);

            var currentUserRole = currentUserMembership.Role;

            try
            {
                var membersQuery = _unitOfWork.GroupMembers.GetQueryable()
                    .AsNoTracking()
                    .Where(gm => gm.GroupID == groupId);

                if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                {
                    var searchTermTrimmed = query.SearchTerm.Trim();
                    membersQuery = membersQuery.Where(m => m.User.FullName != null && m.User.FullName.Contains(searchTermTrimmed));
                }

                var pagedSqlResult = await membersQuery
                    .OrderBy(m => m.Role)
                    .ThenBy(m => m.User.FullName)
                    .Select(m => new GroupMemberListDto
                    {
                        UserId = m.UserID,
                        FullName = m.User.FullName!,
                        AvatarUrl = m.User.AvatarUrl,
                        Role = m.Role,
                        JoinedAt = m.JoinedAt
                    })
                    .ToPagedResultAsync(query.PageNumber, query.PageSize);

                foreach (var member in pagedSqlResult.Items)
                {
                    member.CanManageRole = currentUserRole == EnumGroupRole.Admin
                                           && member.Role < EnumGroupRole.Admin
                                           && member.UserId != currentUserId;

                    member.CanKick = (currentUserRole == EnumGroupRole.Admin && member.Role < EnumGroupRole.Admin && member.UserId != currentUserId) ||
                                     (currentUserRole == EnumGroupRole.Moderator && member.Role < EnumGroupRole.Moderator && member.UserId != currentUserId);
                }

                var memberUserIds = pagedSqlResult.Items.Select(m => m.UserId).ToList();
                if (memberUserIds.Any())
                {
                    var presenceStatuses = await _presenceService.GetStatusesForUsersAsync(memberUserIds);
                    foreach (var member in pagedSqlResult.Items)
                    {
                        member.PresenceStatus = presenceStatuses.GetValueOrDefault(member.UserId, EnumUserPresenceStatus.Offline);
                    }
                }

                return ApiResponse<PagedResult<GroupMemberListDto>>.Ok(pagedSqlResult, "Lấy danh sách thành viên thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách thành viên cho nhóm {GroupId}", groupId);
                return ApiResponse<PagedResult<GroupMemberListDto>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
            }
        }

        public async Task<ApiResponse<PagedResult<PublicGroupDto>>> GetPublicGroupsAsync(GetPublicGroupsQuery query)
        {
            Guid? currentUserId = null;
            if (Guid.TryParse(_currentUser.Id, out var userId))
            {
                currentUserId = userId;
            }

            try
            {
                List<Guid> joinedGroupIds = new List<Guid>();
                if (currentUserId.HasValue)
                {
                    joinedGroupIds = await _unitOfWork.GroupMembers.GetQueryable()
                        .Where(gm => gm.UserID == currentUserId.Value)
                        .Select(gm => gm.GroupID)
                        .ToListAsync();
                }

                var queryable = _unitOfWork.Groups.GetQueryable()
                    .Where(g => !g.IsDeleted &&
                        (g.GroupType == EnumGroupType.Public || (g.GroupType == EnumGroupType.Community && g.Privacy == EnumGroupPrivacy.Public))
                    );

                if (currentUserId.HasValue)
                {
                    queryable = queryable.Where(g => !joinedGroupIds.Contains(g.GroupID));
                }

                // Áp dụng các bộ lọc khác (SearchTerm, FilterType) không đổi
                if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                {
                    var searchTermTrimmed = query.SearchTerm.Trim();
                    queryable = queryable.Where(g => g.GroupName.Contains(searchTermTrimmed));
                }

                queryable = query.FilterType switch
                {
                    MyGroupFilterType.Chat => queryable.Where(g => g.GroupType == EnumGroupType.Public),
                    MyGroupFilterType.Community => queryable.Where(g => g.GroupType == EnumGroupType.Community),
                    _ => queryable
                };

                var projectedQuery = queryable.Select(g => new PublicGroupDto
                {
                    GroupId = g.GroupID,
                    GroupName = g.GroupName,
                    Description = g.Description,
                    GroupAvatarUrl = g.GroupAvatarUrl,
                    MemberCount = g.Members.Count(),
                    GroupType = (g.GroupType == EnumGroupType.Community
                        ? GroupTypeApiDto.Community
                        : GroupTypeApiDto.Chat)
                });

                var pagedResult = await projectedQuery
                    .OrderByDescending(g => g.MemberCount)
                    .ToPagedResultAsync(query.PageNumber, query.PageSize);

                return ApiResponse<PagedResult<PublicGroupDto>>.Ok(pagedResult, "Lấy danh sách nhóm công khai thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách nhóm công khai.");
                return ApiResponse<PagedResult<PublicGroupDto>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
            }
        }

        public async Task<ApiResponse<JoinGroupPublicResponseDTO>> JoinPublicGroupAsync(Guid groupId)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userGuid))
            {
                return ApiResponse<JoinGroupPublicResponseDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");
            }

            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null || group.IsDeleted)
            {
                return ApiResponse<JoinGroupPublicResponseDTO>.Fail("GroupNotFound", "Nhóm không tồn tại.");
            }

            if (group.Privacy != EnumGroupPrivacy.Public)
            {
                return ApiResponse<JoinGroupPublicResponseDTO>.Fail("NotPublicGroup", "Bạn chỉ có thể tự do tham gia các nhóm công khai.");
            }

            var isAlreadyMember = await _unitOfWork.GroupMembers.GetQueryable()
                .AnyAsync(gm => gm.GroupID == groupId && gm.UserID == userGuid);
            if (isAlreadyMember)
            {
                return ApiResponse<JoinGroupPublicResponseDTO>.Fail("AlreadyMember", "Bạn đã là thành viên của nhóm này.");
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var newMember = new GroupMember
                {
                    GroupID = groupId,
                    UserID = userGuid,
                    Role = EnumGroupRole.Member,
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.GroupMembers.AddAsync(newMember);

                var defaultConversation = await _unitOfWork.Conversations
                    .GetQueryable()
                    .AsNoTracking() 
                    .FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);

                if (defaultConversation != null)
                {
                    var newParticipant = new ConversationParticipants
                    {
                        ConversationID = defaultConversation.ConversationID,
                        UserID = userGuid,
                        JoinedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ConversationParticipants.AddAsync(newParticipant);
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync(); 

                var responseDto = new JoinGroupPublicResponseDTO
                {
                    GroupId = groupId,
                    DefaultConversationId = defaultConversation?.ConversationID ?? 0
                };

                return ApiResponse<JoinGroupPublicResponseDTO>.Ok(responseDto, "Bạn đã tham gia nhóm thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error joining public group {GroupId} for user {UserId}", groupId, userGuid);
                return ApiResponse<JoinGroupPublicResponseDTO>.Fail("ServerError", "Đã có lỗi xảy ra, vui lòng thử lại.");
            }
        }

        public async Task<ApiResponse<object>> SendInvitationsAsync(Guid groupId, SendInvitationsDto dto)
        {
            if (!Guid.TryParse(_currentUser.Id, out var inviterGuid))
                return ApiResponse<object>.Fail("Unauthorized", "Người mời không hợp lệ.");

            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null || group.IsDeleted)
                return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.", 404);

            var inviterMembership = await _unitOfWork.GroupMembers.GetQueryable()
                .Include(gm => gm.User) 
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == inviterGuid);

            if (group.Privacy == EnumGroupPrivacy.Private)
            {
                if (inviterMembership == null || inviterMembership.Role == EnumGroupRole.Member)
                    return ApiResponse<object>.Fail("Forbidden", "Bạn không có quyền mời thành viên vào nhóm riêng tư này.");
            }
            else // group.Privacy == EnumGroupPrivacy.Public
            {
                if (inviterMembership == null)
                    return ApiResponse<object>.Fail("Forbidden", "Bạn phải là thành viên của nhóm để gửi lời mời.");
            }

            if (dto.InvitedUserIds == null || !dto.InvitedUserIds.Any())
                return ApiResponse<object>.Fail("Validation", "Danh sách người dùng được mời không được để trống.");

            var successfulInvitations = new List<UserGroupInvitation>();
            var failedUserIds = new List<Guid>();

            // Lấy danh sách các lời mời đã tồn tại cho các user này trong nhóm
            var existingInvitations = await _unitOfWork.UserGroupInvitations.GetQueryable()
                .Where(i => i.GroupID == groupId && dto.InvitedUserIds.Contains(i.InvitedUserID))
                .ToListAsync();

            var existingMemberIds = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => gm.GroupID == groupId && dto.InvitedUserIds.Contains(gm.UserID))
                .Select(gm => gm.UserID)
                .ToListAsync();


            foreach (var invitedId in dto.InvitedUserIds.Distinct()) // Dùng Distinct để tránh xử lý trùng lặp
            {
                // Bỏ qua nếu người này đã là thành viên
                if (existingMemberIds.Contains(invitedId))
                {
                    continue;
                }

                var existingInvitation = existingInvitations.FirstOrDefault(i => i.InvitedUserID == invitedId);

                if (existingInvitation != null)
                {
                    existingInvitation.Status = EnumInvitationStatus.Pending;
                    existingInvitation.InvitedByUserID = inviterGuid;
                    existingInvitation.CreatedAt = DateTime.UtcNow;
                    _unitOfWork.UserGroupInvitations.Update(existingInvitation);
                    successfulInvitations.Add(existingInvitation);
                }
                else
                {
                    var newInvitation = new UserGroupInvitation
                    {
                        GroupID = groupId,
                        InvitedUserID = invitedId,
                        InvitedByUserID = inviterGuid,
                        Status = EnumInvitationStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserGroupInvitations.AddAsync(newInvitation);
                    successfulInvitations.Add(newInvitation);
                }
            }

            if (!successfulInvitations.Any())
            {
                return ApiResponse<object>.Ok(null!, "Tất cả người dùng được chọn đã là thành viên hoặc không hợp lệ.");
            }


            await _unitOfWork.SaveChangesAsync();

            var inviter = inviterMembership?.User;
            if (inviter == null)
            {
                _logger.LogError("Không thể gửi thông báo mời vì thiếu thông tin người mời cho UserID: {InviterId}", inviterGuid);
                return ApiResponse<object>.Ok(null!, $"Đã gửi/cập nhật {successfulInvitations.Count} lời mời thành công (nhưng có lỗi khi gửi thông báo).");
            }

            var eventData = new GroupInvitationEventData(group, inviter);

            foreach (var invitation in successfulInvitations)
            {
                await _notificationService.DispatchNotificationAsync<GroupInvitationNotificationTemplate, GroupInvitationEventData>(
                    targetUserId: invitation.InvitedUserID,
                    eventData: eventData
                );
            }

            return ApiResponse<object>.Ok(null!, $"Đã gửi thành công {successfulInvitations.Count} lời mời.");
        }

        public async Task<ApiResponse<InviteLinkDTO>> CreateInviteLinkAsync(Guid groupId, CreateInviteLinkDTO dto)
        {
            if (!Guid.TryParse(_currentUser.Id, out var creatorGuid))
                return ApiResponse<InviteLinkDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null || group.IsDeleted)
                return ApiResponse<InviteLinkDTO>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");

            var membership = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == creatorGuid);

            if (group.Privacy == EnumGroupPrivacy.Private)
            {
                // QUY TẮC CHO NHÓM RIÊNG TƯ: Phải là Admin/Mod
                if (membership == null || membership.Role == EnumGroupRole.Member)
                    return ApiResponse<InviteLinkDTO>.Fail("Forbidden", "Bạn không có quyền tạo link mời cho nhóm riêng tư này.");
            }
            else // group.Privacy == EnumGroupPrivacy.Public
            {
                if (membership == null)
                    return ApiResponse<InviteLinkDTO>.Fail("Forbidden", "Bạn phải là thành viên của nhóm để tạo link mời.");
            }

            string newCode;
            do
            {
                newCode = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Replace("=", "").Replace("+", "").Replace("/", "").Substring(0, 8);
            }
            while (await _unitOfWork.GroupInvitations.GetQueryable().AnyAsync(i => i.InvitationCode == newCode));

            var newLink = new GroupInvitations
            {
                InvitationCode = newCode,
                GroupID = groupId,
                CreatedByUserID = creatorGuid,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = dto.ExpiresInHours.HasValue ? DateTime.UtcNow.AddHours(dto.ExpiresInHours.Value) : null,
                MaxUses = dto.MaxUses,
                IsActive = true
            };

            await _unitOfWork.GroupInvitations.AddAsync(newLink);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = new InviteLinkDTO { InvitationCode = newCode};
            return ApiResponse<InviteLinkDTO>.Ok(responseDto, "Tạo link mời thành công.");
        }

        public async Task<ApiResponse<GroupDetailsDTO>> GetGroupDetailsByIdAsync(Guid groupId)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return ApiResponse<GroupDetailsDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

            var userMembership = await _unitOfWork.GroupMembers.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);

            if (userMembership == null)
                return ApiResponse<GroupDetailsDTO>.Fail("FORBIDDEN", "Bạn không phải là thành viên của nhóm này.");

            var currentUserRole = userMembership.Role;

            var groupDetailsDto = await _unitOfWork.Groups.GetQueryable()
                .Where(g => g.GroupID == groupId && !g.IsDeleted)
                .Select(g => new GroupDetailsDTO
                {
                    GroupId = g.GroupID,
                    GroupName = g.GroupName,
                    Description = g.Description,
                    GroupAvatarUrl = g.GroupAvatarUrl,
                    MemberCount = g.Members.Count(),
                    GroupType = (g.GroupType == EnumGroupType.Community ? GroupTypeApiDto.Community : GroupTypeApiDto.Chat),
                    Privacy = g.Privacy,
                    IsArchived = g.IsArchived,
                    CanEdit = currentUserRole == EnumGroupRole.Admin || currentUserRole == EnumGroupRole.Moderator,
                    CanArchive = currentUserRole == EnumGroupRole.Admin,
                    CanDelete = currentUserRole == EnumGroupRole.Admin,
                    // Quy tắc:
                    // - Nếu nhóm là Public, bất kỳ thành viên nào cũng có quyền mời.
                    // - Nếu nhóm là Private, chỉ Admin/Mod mới có quyền mời.
                    CanInviteMembers = (g.Privacy == EnumGroupPrivacy.Public) ||
                               (g.Privacy == EnumGroupPrivacy.Private && currentUserRole > EnumGroupRole.Member)
                })
                .FirstOrDefaultAsync();

            if (groupDetailsDto == null)
            {
                return ApiResponse<GroupDetailsDTO>.Fail("NotFound", "Không tìm thấy nhóm.");
            }

            return ApiResponse<GroupDetailsDTO>.Ok(groupDetailsDto);
        }

        public async Task<ApiResponse<GroupDetailsDTO>> GetGroupDetailsByIdAsyncPreView(Guid groupId)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return ApiResponse<GroupDetailsDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

            var groupDetailsDto = await _unitOfWork.Groups.GetQueryable()
                .Where(g => g.GroupID == groupId && !g.IsDeleted)
                .Select(g => new GroupDetailsDTO
                {
                    GroupId = g.GroupID,
                    GroupName = g.GroupName,
                    Description = g.Description,
                    GroupAvatarUrl = g.GroupAvatarUrl,
                    MemberCount = g.Members.Count(),
                    GroupType = (g.GroupType == EnumGroupType.Community ? GroupTypeApiDto.Community : GroupTypeApiDto.Chat),
                    Privacy = g.Privacy,
                })
                .FirstOrDefaultAsync();

            if (groupDetailsDto == null)
            {
                return ApiResponse<GroupDetailsDTO>.Fail("NotFound", "Không tìm thấy nhóm.");
            }

            return ApiResponse<GroupDetailsDTO>.Ok(groupDetailsDto);
        }

        public async Task<ApiResponse<UpdateRoleResponseDTO>> ManageMemberRoleAsync(Guid groupId, Guid memberId, ManageMemberDTO dto)
        {
            if (!Guid.TryParse(_currentUser.Id, out var callerGuid))
                return ApiResponse<UpdateRoleResponseDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

            var callerMembership = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == callerGuid);

            var targetMember = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == memberId);

            if (callerMembership?.Role != EnumGroupRole.Admin)
                return ApiResponse<UpdateRoleResponseDTO>.Fail("Forbidden", "Chỉ quản trị viên mới có quyền thực hiện hành động này.");

            if (targetMember == null)
                return ApiResponse<UpdateRoleResponseDTO>.Fail("TargetNotFound", "Không tìm thấy thành viên này trong nhóm.");

            if (targetMember.Role == EnumGroupRole.Admin)
                return ApiResponse<UpdateRoleResponseDTO>.Fail("CannotChangeAdminRole", "Không thể thay đổi vai trò của Quản trị viên khác.");

            var newRoleName = string.Empty;
            switch (dto.Action)
            {
                case ManageMemberAction.PromoteToModerator:
                    if (targetMember.Role != EnumGroupRole.Member)
                        return ApiResponse<UpdateRoleResponseDTO>.Fail("InvalidAction", "Chỉ có thể thăng cấp cho Thành viên.");

                    targetMember.Role = EnumGroupRole.Moderator;
                    newRoleName = nameof(EnumGroupRole.Moderator);
                    break;

                case ManageMemberAction.DemoteToMember:
                    if (targetMember.Role != EnumGroupRole.Moderator)
                        return ApiResponse<UpdateRoleResponseDTO>.Fail("InvalidAction", "Chỉ có thể hạ cấp cho Kiểm duyệt viên.");

                    targetMember.Role = EnumGroupRole.Member;
                    newRoleName = nameof(EnumGroupRole.Member);
                    break;

                default:
                    return ApiResponse<UpdateRoleResponseDTO>.Fail("InvalidAction", "Hành động không được hỗ trợ.");
            }

            _unitOfWork.GroupMembers.Update(targetMember);
            await _unitOfWork.SaveChangesAsync();

            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            var targetUser = await _unitOfWork.Users.GetByIdAsync(memberId);
            var callerUser = await _unitOfWork.Users.GetByIdAsync(callerGuid);

            if (group != null && targetUser != null && callerUser != null)
            {
                var eventData = new RoleChangedEventData(group, newRoleName);
                await _notificationService.DispatchNotificationAsync<RoleChangedNotificationTemplate, RoleChangedEventData>(
                    memberId, 
                    eventData
                );

                var conversation = await _unitOfWork.Conversations.GetQueryable().AsNoTracking().FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);
                if (conversation != null)
                {
                    string actionText = dto.Action == ManageMemberAction.PromoteToModerator ? "thăng cấp" : "hạ cấp";
                    string roleText = dto.Action == ManageMemberAction.PromoteToModerator ? "Kiểm duyệt viên" : "Thành viên";

                    var systemMessageContent = $"{callerUser.FullName} đã {actionText} cho {targetUser.FullName} thành {roleText}.";
                    await _messageService.SendSystemMessageAsync(conversation.ConversationID, systemMessageContent);
                }
            }

            var responseDto = new UpdateRoleResponseDTO { NewRole = newRoleName };
            return ApiResponse<UpdateRoleResponseDTO>.Ok(responseDto, "Thay đổi vai trò thành công.");
        }

        public async Task<ApiResponse<object>> LeaveGroupAsync(Guid groupId)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userGuid))
                return ApiResponse<object>.Fail("Unauthorized", "Người dùng không hợp lệ.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var membership = await _unitOfWork.GroupMembers.GetQueryable()
                    .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userGuid);

                if (membership == null)
                    return ApiResponse<object>.Fail("NotAMember", "Bạn không phải là thành viên của nhóm này.");

                // QUAN TRỌNG: Logic kiểm tra Admin cuối cùng
                if (membership.Role == EnumGroupRole.Admin)
                {
                    var otherAdminsCount = await _unitOfWork.GroupMembers.GetQueryable()
                        .CountAsync(
                        gm => gm.GroupID == groupId &&
                              gm.Role == EnumGroupRole.Admin &&
                              gm.UserID != userGuid);

                    if (otherAdminsCount == 0)
                    {
                        // Trả về mã lỗi đặc biệt để Frontend xử lý
                        return ApiResponse<object>.Fail("LAST_ADMIN_LEAVE_ATTEMPT", "Bạn là quản trị viên cuối cùng. Vui lòng chuyển quyền cho người khác trước khi rời nhóm.");
                    }
                }

                // Xử lý rời nhóm bình thường
                var participationsToRemove = await _unitOfWork.ConversationParticipants.GetQueryable()
                    .Where(cp => cp.UserID == userGuid && cp.Conversation!.ExplicitGroupID == groupId)
                    .ToListAsync();
                if (participationsToRemove.Any())
                    _unitOfWork.ConversationParticipants.RemoveRange(participationsToRemove);

                _unitOfWork.GroupMembers.Remove(membership);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
                var conversation = await _unitOfWork.Conversations.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);

                if (conversation != null)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userGuid);
                    if (user != null)
                    {
                        var systemMessageContent = $"{user.FullName} đã rời khỏi nhóm.";
                        await _messageService.SendSystemMessageAsync(conversation.ConversationID, systemMessageContent);
                    }
                }


                return ApiResponse<object>.Ok(null!, "Rời nhóm thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error leaving group {GroupId} for user {UserId}", groupId, userGuid);
                return ApiResponse<object>.Fail("ServerError", "Có lỗi xảy ra, vui lòng thử lại.");
            }
        }

        public async Task<ApiResponse<object>> TransferAndLeaveAsync(Guid groupId, TransferAndLeaveDTO dto)
        {
            if (!Guid.TryParse(_currentUser.Id, out var currentAdminGuid))
                return ApiResponse<object>.Fail("Unauthorized", "Người dùng không hợp lệ.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var groupMembers = await _unitOfWork.GroupMembers.GetQueryable()
                    .Where(gm => gm.GroupID == groupId)
                    .ToListAsync();

                var currentAdminMembership = groupMembers.FirstOrDefault(m => m.UserID == currentAdminGuid);
                var newAdminMembership = groupMembers.FirstOrDefault(m => m.UserID == dto.NewAdminUserId);

                if (currentAdminMembership?.Role != EnumGroupRole.Admin)
                    return ApiResponse<object>.Fail("Forbidden", "Bạn không có quyền thực hiện hành động này.");

                var adminCount = groupMembers.Count(m => m.Role == EnumGroupRole.Admin);
                if (adminCount != 1)
                    return ApiResponse<object>.Fail("NotLastAdmin", "Hành động này chỉ dành cho quản trị viên cuối cùng của nhóm.");

                if (newAdminMembership == null)
                    return ApiResponse<object>.Fail("TargetNotFound", "Người được chọn không phải là thành viên của nhóm.");

                // Thăng cấp cho người kế nhiệm
                newAdminMembership.Role = EnumGroupRole.Admin;

                // Xóa người dùng hiện tại khỏi các cuộc trò chuyện
                var participationsToRemove = await _unitOfWork.ConversationParticipants.GetQueryable()
                    .Where(cp => cp.UserID == currentAdminGuid && cp.Conversation!.ExplicitGroupID == groupId)
                    .ToListAsync();
                if (participationsToRemove.Any())
                    _unitOfWork.ConversationParticipants.RemoveRange(participationsToRemove);

                // Xóa bản ghi thành viên của người dùng hiện tại
                _unitOfWork.GroupMembers.Remove(currentAdminMembership);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
                var newAdminUser = await _unitOfWork.Users.GetByIdAsync(dto.NewAdminUserId);

                if (group != null && newAdminUser != null)
                {
                    // 1. Gửi thông báo cá nhân cho admin mới
                    var eventData = new AdminPromotionEventData(group);
                    await _notificationService.DispatchNotificationAsync<AdminPromotionNotificationTemplate, AdminPromotionEventData>(
                        dto.NewAdminUserId,
                        eventData
                    );

                    // 2. Gửi thông báo hệ thống vào kênh chat (như đã làm ở lần trước)
                    var oldAdmin = await _unitOfWork.Users.GetByIdAsync(currentAdminGuid);
                    if (oldAdmin != null)
                    {
                        var conversation = await _unitOfWork.Conversations.GetQueryable().AsNoTracking().FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);
                        if (conversation != null)
                        {
                            var systemMessageContent = $"{oldAdmin.FullName} đã chuyển quyền Quản trị viên cho {newAdminUser.FullName} và rời khỏi nhóm.";
                            await _messageService.SendSystemMessageAsync(conversation.ConversationID, systemMessageContent);
                        }
                    }
                }

                return ApiResponse<object>.Ok(null!, "Chuyển quyền và rời nhóm thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error transferring admin role and leaving group {GroupId} for user {UserId}", groupId, currentAdminGuid);
                return ApiResponse<object>.Fail("ServerError", "Có lỗi xảy ra, vui lòng thử lại.");
            }
        }

        public async Task<ApiResponse<List<MentionSuggestionDTO>>> GetMentionSuggestionsAsync(Guid groupId, string? searchTerm)
        {
            if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
                return ApiResponse<List<MentionSuggestionDTO>>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ.");

            var isMember = await _unitOfWork.GroupMembers.GetQueryable()
                .AnyAsync(gm => gm.GroupID == groupId && gm.UserID == currentUserId);

            if (!isMember)
                return ApiResponse<List<MentionSuggestionDTO>>.Fail("FORBIDDEN", "Bạn không có quyền xem danh sách thành viên nhóm này.");

            var query = _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm =>
                    gm.GroupID == groupId &&
                    gm.UserID != currentUserId 
                );

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(gm =>
                    gm.User!.FullName!.ToLower().Contains(term) ||
                    gm.User!.Email!.ToLower().Contains(term) 
                );
            }

            var suggestions = await query
                .Select(gm => new MentionSuggestionDTO
                {
                    UserId = gm.UserID,
                    FullName = gm.User!.FullName!,
                    AvatarUrl = gm.User.AvatarUrl,
                    PresenceStatus = gm.User.PresenceStatus
                })
                .Take(10) 
                .ToListAsync();

            return ApiResponse<List<MentionSuggestionDTO>>.Ok(suggestions);
        }

        public async Task<ApiResponse<UpdateGroupAvatarResponseDTO>> UpdateGroupAvatarAsync(Guid groupId, IFormFile avatarFile)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return ApiResponse<UpdateGroupAvatarResponseDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

            var membership = await _unitOfWork.GroupMembers.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);

            if (membership == null || membership.Role == EnumGroupRole.Member)
                return ApiResponse<UpdateGroupAvatarResponseDTO>.Fail("FORBIDDEN", "Bạn không có quyền thay đổi ảnh đại diện của nhóm này.");

            var uploadResponse = await _fileService.UploadAvatarAsync(avatarFile, $"group-avatars/{groupId}", "GroupAvatar");
            if (!uploadResponse.Success)
            {
                return ApiResponse<UpdateGroupAvatarResponseDTO>.Fail(uploadResponse.Errors);
            }

            var group = await _unitOfWork.Groups.GetQueryable()
                .Include(g => g.Conversation)
                .FirstOrDefaultAsync(g => g.GroupID == groupId);

            if (group == null)
                return ApiResponse<UpdateGroupAvatarResponseDTO>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");

            // 4. Cập nhật CSDL
            var oldAvatarUrl = group.GroupAvatarUrl;
            group.GroupAvatarUrl = uploadResponse.Data!.Url;
            
            var conversation = await _unitOfWork.Conversations.GetQueryable()
                .FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);

            if (conversation != null)
            {
                conversation.AvatarUrl = uploadResponse.Data!.Url;
            }

            await _unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrEmpty(oldAvatarUrl))
            {
                _backgroundJobClient.Enqueue<IFileStorageService>(s => s.DeleteAsync(oldAvatarUrl));
            }

            if (group.Conversation != null)
            {
                var systemMessageContent = $"{_currentUser.FullName} đã thay đổi ảnh đại diện của nhóm.";
                _backgroundJobClient.Enqueue<IMessageService>(s =>
                    s.SendSystemMessageAsync(group.Conversation.ConversationID, systemMessageContent));
            }

            var responseDto = new UpdateGroupAvatarResponseDTO { NewAvatarUrl = uploadResponse.Data!.Url };
            return ApiResponse<UpdateGroupAvatarResponseDTO>.Ok(responseDto, "Cập nhật ảnh đại diện nhóm thành công.");
        }

        public async Task<ApiResponse<object>> AddMemberAsync(Guid groupId, Guid userIdToAdd)
        {
            if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
                return ApiResponse<object>.Fail("UNAUTHORIZED", "Không xác thực được người dùng.", 401);

            var membership = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == currentUserId);

            //if (membership == null || membership.Role == EnumGroupRole.Member)
            //    return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền thêm thành viên vào nhóm này.", 403);

            var userToAdd = await _unitOfWork.Users.GetByIdAsync(userIdToAdd);
            if (userToAdd == null || userToAdd.IsDeleted)
                return ApiResponse<object>.Fail("USER_NOT_FOUND", "Người dùng được mời không tồn tại.", 404);

            var isAlreadyMember = await _unitOfWork.GroupMembers.GetQueryable()
                .AnyAsync(gm => gm.GroupID == groupId && gm.UserID == userIdToAdd);
            if (isAlreadyMember)
                return ApiResponse<object>.Fail("ALREADY_MEMBER", "Người dùng này đã là thành viên của nhóm.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var newMember = new GroupMember
                {
                    GroupID = groupId,
                    UserID = userIdToAdd,
                    Role = EnumGroupRole.Member,
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.GroupMembers.AddAsync(newMember);

                var group = await _unitOfWork.Groups.GetQueryable()
                    .Include(g => g.Conversation)
                    .FirstOrDefaultAsync(g => g.GroupID == groupId);

                var conversation = await _unitOfWork.Conversations.GetQueryable()
                    .FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);

                // BƯỚC 3: Nếu nhóm này có cuộc trò chuyện, thêm thành viên vào đó
                if (conversation != null)
                {
                    var newParticipant = new ConversationParticipants
                    {
                        ConversationID = conversation.ConversationID,
                        UserID = userIdToAdd,
                        JoinedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.ConversationParticipants.AddAsync(newParticipant);
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                //var group2 = await _unitOfWork.Groups.GetByIdAsync(groupId);
                var addedByUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);

                // Gửi thông báo cho người được thêm
                if (group != null && addedByUser != null)
                {
                    var eventData = new UserAddedToGroupEventData(group, addedByUser);

                    await _notificationService.DispatchNotificationAsync<UserAddedToGroupNotificationTemplate, UserAddedToGroupEventData>(
                        userIdToAdd,
                        eventData
                    );
                }
                // Chỉ gửi nếu nhóm này có kênh chat
                if (group?.Conversation != null)
                {
                    var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                    var addedUser = await _unitOfWork.Users.GetByIdAsync(userIdToAdd);

                    if (currentUser != null && addedUser != null)
                    {
                        var systemMessageContent = $"{currentUser.FullName} đã thêm {addedUser.FullName} vào nhóm.";
                        await _messageService.SendSystemMessageAsync(group.Conversation.ConversationID, systemMessageContent);
                    }
                }
                return ApiResponse<object>.Ok(null, "Thêm thành viên thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi thêm thành viên vào nhóm {GroupId}", groupId);
                return ApiResponse<object>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
            }
        }

        public async Task<ApiResponse<CreateGroupsResponseDTO>> CreateChatGroupAsync(CreateChatGroupDto dto)
        {
            if (!Guid.TryParse(_currentUser.Id, out var creatorGuid))
                return ApiResponse<CreateGroupsResponseDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

            // Validation nghiệp vụ: Nhóm chat chỉ có thể là Public hoặc Private
            if (dto.GroupType == EnumGroupType.Community)
            {
                return ApiResponse<CreateGroupsResponseDTO>.Fail("Validation", "Loại nhóm không hợp lệ cho nhóm trò chuyện.");
            }
            string? avatarUrl = null;
            if (dto.AvatarFile != null)
            {
                var uploadResponse = await _fileService.UploadAvatarAsync(dto.AvatarFile, "group-avatars", "GroupAvatar");
                if (!uploadResponse.Success)
                {
                    return ApiResponse<CreateGroupsResponseDTO>.Fail(uploadResponse.Errors);
                }
                avatarUrl = uploadResponse.Data!.Url;
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var newGroup = _mapper.Map<Group>(dto);
                newGroup.CreatedByUserID = creatorGuid;
                newGroup.GroupAvatarUrl = avatarUrl ?? "";
                newGroup.Privacy = dto.GroupType == EnumGroupType.Public ? EnumGroupPrivacy.Public : EnumGroupPrivacy.Private;

                var defaultConversation = new Conversation
                {
                    ConversationType = EnumConversationType.Group,
                    Title = newGroup.GroupName,
                    AvatarUrl = newGroup.GroupAvatarUrl,
                    Group = newGroup 
                };

                var adminMembership = new GroupMember
                {
                    Group = newGroup,
                    UserID = creatorGuid,
                    Role = EnumGroupRole.Admin,
                    JoinedAt = DateTime.UtcNow
                };

                var participant = new ConversationParticipants
                {
                    Conversation = defaultConversation,
                    UserID = creatorGuid,
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.Groups.AddAsync(newGroup);
                await _unitOfWork.Conversations.AddAsync(defaultConversation);
                await _unitOfWork.GroupMembers.AddAsync(adminMembership);
                await _unitOfWork.ConversationParticipants.AddAsync(participant);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                var responseDto = new CreateGroupsResponseDTO
                {
                    GroupId = newGroup.GroupID,
                    GroupName = newGroup.GroupName,
                    DefaultConversationId = defaultConversation.ConversationID
                };

                var sender = _currentUser.FullName;
                await _adminNotificationService.CreateAndBroadcastNotificationAsync(
                    EnumAdminNotificationType.NewGroupCreated,
                    $"Người dùng '{sender}' vừa tạo nhóm chat mới: '{newGroup.GroupName}'.",
                    $"/admin/groups/{newGroup.GroupID}/details",
                    creatorGuid
                    );

                return ApiResponse<CreateGroupsResponseDTO>.Ok(responseDto, "Tạo nhóm chat thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating chat group for user {UserId}", creatorGuid);
                return ApiResponse<CreateGroupsResponseDTO>.Fail("ServerError", "Đã có lỗi xảy ra trong quá trình tạo nhóm.");
            }
        }

        public async Task<ApiResponse<CreateGroupsResponseDTO>> CreateCommunityGroupAsync(CreateCommunityGroupDto dto)
        {
            if (!Guid.TryParse(_currentUser.Id, out var creatorGuid))
                return ApiResponse<CreateGroupsResponseDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

            string? avatarUrl = null;
            if (dto.AvatarFile != null)
            {
                var uploadResponse = await _fileService.UploadAvatarAsync(dto.AvatarFile, "group-avatars", "GroupAvatar");
                if (!uploadResponse.Success)
                {
                    return ApiResponse<CreateGroupsResponseDTO>.Fail(uploadResponse.Errors);
                }
                avatarUrl = uploadResponse.Data!.Url;
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var newGroup = _mapper.Map<Group>(dto);
                newGroup.CreatedByUserID = creatorGuid;
                newGroup.GroupAvatarUrl = avatarUrl ?? "";
                newGroup.GroupType = EnumGroupType.Community; 
                newGroup.Privacy = dto.Privacy; 

                var adminMembership = new GroupMember
                {
                    Group = newGroup,
                    UserID = creatorGuid,
                    Role = EnumGroupRole.Admin,
                    JoinedAt = DateTime.UtcNow
                };

                await _unitOfWork.Groups.AddAsync(newGroup);
                await _unitOfWork.GroupMembers.AddAsync(adminMembership);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                var responseDto = new CreateGroupsResponseDTO
                {
                    GroupId = newGroup.GroupID,
                    GroupName = newGroup.GroupName,
                    DefaultConversationId = 0 
                };
                var sender = _currentUser.FullName;

                await _adminNotificationService.CreateAndBroadcastNotificationAsync(
                    EnumAdminNotificationType.NewGroupCreated,
                    $"Người dùng '{sender}' vừa tạo cộng đồng mới: '{newGroup.GroupName}'.",
                    $"/admin/groups/{newGroup.GroupID}/details",
                    creatorGuid
                    );
                return ApiResponse<CreateGroupsResponseDTO>.Ok(responseDto, "Tạo cộng đồng thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating community group for user {UserId}", creatorGuid);
                return ApiResponse<CreateGroupsResponseDTO>.Fail("ServerError", "Đã có lỗi xảy ra trong quá trình tạo cộng đồng.");
            }
        }

        public async Task<ApiResponse<object>> UpdateGroupInfoAsync(Guid groupId, UpdateGroupInfoDto dto)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

            var group = await _unitOfWork.Groups.GetQueryable()
                .Include(g => g.Conversation) 
                .FirstOrDefaultAsync(g => g.GroupID == groupId && !g.IsDeleted);

            if (group == null)
                return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.", 404);

            var membership = await _unitOfWork.GroupMembers.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);
            if (membership == null || membership.Role == EnumGroupRole.Member)
                return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền chỉnh sửa thông tin nhóm này.", 403);

            group.GroupName = dto.GroupName;
            group.Description = dto.Description;
            group.Privacy = dto.Privacy;
            group.UpdatedAt = DateTime.UtcNow;

            if (group.Conversation != null)
            {
                group.Conversation.Title = dto.GroupName;
            }

            await _unitOfWork.SaveChangesAsync();

            if (group.Conversation != null)
            {
                var systemMessageContent = $"{_currentUser.FullName} đã cập nhật thông tin nhóm.";
                _backgroundJobClient.Enqueue<IMessageService>(s =>
                    s.SendSystemMessageAsync(group.Conversation.ConversationID, systemMessageContent));
            }

            return ApiResponse<object>.Ok(null, "Cập nhật thông tin nhóm thành công.");
        }

        public async Task<ApiResponse<object>> KickMemberAsync(Guid groupId, Guid userIdToKick)
        {
            if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
                return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

            if (currentUserId == userIdToKick)
                return ApiResponse<object>.Fail("CANNOT_KICK_SELF", "Bạn không thể tự xóa chính mình khỏi nhóm.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var memberships = await _unitOfWork.GroupMembers.GetQueryable()
                    .Where(gm => gm.GroupID == groupId && (gm.UserID == currentUserId || gm.UserID == userIdToKick))
                    .ToListAsync();

                var currentUserMembership = memberships.FirstOrDefault(m => m.UserID == currentUserId);
                var targetUserMembership = memberships.FirstOrDefault(m => m.UserID == userIdToKick);

                if (currentUserMembership == null)
                    return ApiResponse<object>.Fail("NOT_A_MEMBER", "Bạn không phải là thành viên của nhóm này.", 403);

                if (targetUserMembership == null)
                    return ApiResponse<object>.Fail("TARGET_NOT_FOUND", "Người dùng bạn muốn xóa không phải là thành viên của nhóm.", 404);

                bool hasPermission = currentUserMembership.Role switch
                {
                    EnumGroupRole.Admin => targetUserMembership.Role == EnumGroupRole.Moderator || targetUserMembership.Role == EnumGroupRole.Member,
                    EnumGroupRole.Moderator => targetUserMembership.Role == EnumGroupRole.Member,
                    _ => false
                };

                if (!hasPermission)
                    return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có đủ quyền hạn để xóa thành viên này.", 403);

                _unitOfWork.GroupMembers.Remove(targetUserMembership);

                var conversation = await _unitOfWork.Conversations.GetQueryable().FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);
                if (conversation != null)
                {
                    var participant = await _unitOfWork.ConversationParticipants.GetQueryable()
                        .FirstOrDefaultAsync(p => p.ConversationID == conversation.ConversationID && p.UserID == userIdToKick);
                    if (participant != null)
                    {
                        _unitOfWork.ConversationParticipants.Remove(participant);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
                var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                var kickedUser = await _unitOfWork.Users.GetByIdAsync(userIdToKick);

                if (group != null && currentUser != null && kickedUser != null)
                {
                    // 1. Gửi tin nhắn hệ thống vào kênh chat (đã có)
                    if (conversation != null)
                    {
                        var systemMessageContent = $"{kickedUser.FullName} đã bị xóa khỏi nhóm bởi {currentUser.FullName}.";
                        await _messageService.SendSystemMessageAsync(conversation.ConversationID, systemMessageContent);
                    }

                    // 2. Gửi thông báo cá nhân cho người bị kick
                    var eventData = new UserKickedEventData(group, currentUser);
                    await _notificationService.DispatchNotificationAsync<UserKickedNotificationTemplate, UserKickedEventData>(
                        userIdToKick,
                        eventData
                    );
                }

                return ApiResponse<object>.Ok(null, "Xóa thành viên thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xóa thành viên {UserIdToKick} khỏi nhóm {GroupId}", userIdToKick, groupId);
                return ApiResponse<object>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
            }
        }
        public async Task<ApiResponse<object>> DeleteGroupAsync(Guid groupId)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null || group.IsDeleted) 
                return ApiResponse<object>.Ok(null, "Nhóm đã được xóa hoặc không tồn tại.");

            var membership = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);
            if (membership == null || membership.Role != EnumGroupRole.Admin)
                return ApiResponse<object>.Fail("FORBIDDEN", "Chỉ quản trị viên mới có quyền xóa nhóm này.");

            group.IsDeleted = true;
            group.DeletedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _backgroundJobClient.Enqueue<IDataRetentionService>(s => s.PermanentlyDeleteGroupAsync(groupId));

            _logger.LogInformation("Group {GroupId} was soft-deleted by user {UserId}", groupId, userId);

            return ApiResponse<object>.Ok(null, $"Nhóm đã được xóa và sẽ bị xóa vĩnh viễn sa");
        }

        public async Task<ApiResponse<object>> ArchiveGroupAsync(Guid groupId)
        {
            return await SetGroupArchiveStatusAsync(groupId, true);
        }

        public async Task<ApiResponse<object>> UnarchiveGroupAsync(Guid groupId)
        {
            return await SetGroupArchiveStatusAsync(groupId, false);
        }

        // --- PHƯƠNG THỨC HELPER DÙNG CHUNG LƯU TRỮ ---
        private async Task<ApiResponse<object>> SetGroupArchiveStatusAsync(Guid groupId, bool isArchived)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null || group.IsDeleted)
                return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");

            var membership = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);
            if (membership == null || membership.Role != EnumGroupRole.Admin)
                return ApiResponse<object>.Fail("FORBIDDEN", "Chỉ quản trị viên mới có quyền thực hiện hành động này.");

            if (group.IsArchived == isArchived)
            {
                var status = isArchived ? "lưu trữ" : "hoạt động";
                return ApiResponse<object>.Ok(null, $"Nhóm đã ở trạng thái {status}.");
            }

            group.IsArchived = isArchived;
            group.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            var conversation = await _unitOfWork.Conversations.GetQueryable().AsNoTracking().FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);
            if (conversation != null)
            {
                var actionText = isArchived ? "lưu trữ" : "khôi phục";
                var systemMessageContent = $"{_currentUser.FullName} đã {actionText} nhóm.";
                await _messageService.SendSystemMessageAsync(conversation.ConversationID, systemMessageContent);
            }

            var successMessage = isArchived ? "Lưu trữ nhóm thành công." : "Khôi phục nhóm thành công.";
            return ApiResponse<object>.Ok(null, successMessage);
        }
    }
}
