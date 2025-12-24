using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.Notifications.Templates;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Infastructure.Caching;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace FastBiteGroupMCA.Infastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly StorageStrategy _storageStrategy;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IUserPresenceService _presenceService;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly IMessagesRepository _messageRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly long _maxAvatarSizeBytes;

    public UserService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UserService> logger,
        StorageStrategy storageStrategy,
        ICurrentUser currentUser,
        IConfiguration configuration,
        IBackgroundJobClient backgroundJobClient,
        ICacheService cacheService,
        IUserPresenceService presenceService,
        IMessagesRepository messagesRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _storageStrategy = storageStrategy;
        _currentUser = currentUser;
        _backgroundJobClient = backgroundJobClient;
        _maxAvatarSizeBytes = configuration.GetValue<long>("FileUploadSettings:MaxAvatarSizeMb", 2) * 1024 * 1024;
        _cacheService = cacheService;
        _presenceService = presenceService;
        _messageRepo = messagesRepository;
    }
    public async Task<ApiResponse<MyProfileDto>> GetMyProfileAsync()
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<MyProfileDto>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var profileData = await _unitOfWork.Users.GetQueryable()
            .Where(u => u.Id == userId)
            .Select(u => new MyProfileDto
            {
                UserId = u.Id,
                FullName = u.FullName!,
                Email = u.Email!,
                DateOfBirth = u.DateOfBirth,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                TwoFactorEnabled = u.TwoFactorEnabled,
                CreatedAt = u.CreatedAt,
                UpdateAt = u.UpdatedAt ?? DateTime.MinValue,
                MessagingPrivacy = u.MessagingPrivacy,
                Groups = u.GroupMemberships
                            .OrderByDescending(gm => gm.JoinedAt)
                            .Take(5)
                            .Select(gm => new MyGroupInfoDto
                            {
                                GroupId = gm.Group.GroupID,
                                GroupName = gm.Group.GroupName,
                                GroupAvatarUrl = gm.Group.GroupAvatarUrl
                            }).ToList(),
                RecentPosts = u.AuthoredPosts
                                .OrderByDescending(p => p.CreatedAt)
                                .Take(5)
                                .Select(p => new MyPostInfoDto
                                {
                                    PostId = p.PostID,
                                    Title = p.Title,
                                    CreatedAt = p.CreatedAt
                                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (profileData == null)
            return ApiResponse<MyProfileDto>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
        var presenceStatus = await _presenceService.GetUserStatusAsync(userId);
        profileData.PresenceStatus = presenceStatus;

        return ApiResponse<MyProfileDto>.Ok(profileData);
    }

    public async Task<ApiResponse<UserDto>> UpdateProfileInfoAsync(UpdateUserADDto request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<UserDto>.Fail("UNAUTHORIZED", "Người dùng không được xác thực.");

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null) return ApiResponse<UserDto>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        user.FisrtName = request.FirstName;
        user.LastName = request.LastName;
        user.FullName = $"{request.FirstName} {request.LastName}".Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.Bio = request.Bio;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ApiResponse<UserDto>.Fail(result.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList());

        await _userManager.SetTwoFactorEnabledAsync(user, request.TwoFactorEnabled);

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = await _userManager.GetRolesAsync(user);

        return ApiResponse<UserDto>.Ok(userDto, "Cập nhật thông tin thành công.");
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(PagedRequestDto request)
    {
        try
        {
            var users = await _unitOfWork.Users.GetPagedUsersAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm);

            var totalCount = await _unitOfWork.Users.GetTotalUsersCountAsync(request.SearchTerm);

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(userDto);
            }

            var result = new PagedResult<UserDto>(
                userDtos.AsReadOnly(),
                totalCount,
                request.PageNumber,
                request.PageSize);

            return ApiResponse<PagedResult<UserDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users list");
            return ApiResponse<PagedResult<UserDto>>.Fail("GET_USERS_ERROR", "Đã có lỗi xảy ra khi lấy danh sách người dùng.");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<UserDto>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            }

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);

            return ApiResponse<UserDto>.Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return ApiResponse<UserDto>.Fail("GET_USER_ERROR", "Đã có lỗi xảy ra khi lấy thông tin người dùng.");
        }
    }

    public async Task<ApiResponse<string>> ChangeUserRoleAsync(Guid userId, Guid newRoleId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<string>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            }

            var role = await _roleManager.FindByIdAsync(newRoleId.ToString());
            if (role == null)
            {
                return ApiResponse<string>.Fail("ROLE_NOT_FOUND", "Không tìm thấy vai trò.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = removeResult.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
                    return ApiResponse<string>.Fail(errors);
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, role.Name!);
            if (!addResult.Succeeded)
            {
                var errors = addResult.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
                return ApiResponse<string>.Fail(errors);
            }

            _logger.LogInformation("Changed role of user {UserId} to {Role}", userId, role.Name);
            return ApiResponse<string>.Ok("Cập nhật vai trò người dùng thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing role for user {UserId}", userId);
            return ApiResponse<string>.Fail("CHANGE_ROLE_ERROR", "Đã có lỗi xảy ra khi cập nhật vai trò.");
        }
    }

    public async Task<ApiResponse<UpdateAvatarResponseDTO>> UpdateUserAvatarAsync(IFormFile avatarFile)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<UpdateAvatarResponseDTO>.Fail("UNAUTHORIZED", "Người dùng không được xác thực.");

        if (avatarFile == null || avatarFile.Length == 0)
            return ApiResponse<UpdateAvatarResponseDTO>.Fail("FILE_EMPTY", "Không có file nào được chọn.");

        if (avatarFile.Length > _maxAvatarSizeBytes)
            return ApiResponse<UpdateAvatarResponseDTO>.Fail("FILE_TOO_LARGE", "Kích thước ảnh vượt quá giới hạn cho phép.");

        if (!avatarFile.ContentType.ToLower().StartsWith("image/"))
            return ApiResponse<UpdateAvatarResponseDTO>.Fail("INVALID_FILE_TYPE", "Chỉ chấp nhận file định dạng ảnh.");

        var user = await _userManager.FindByIdAsync(currentUserId.ToString());
        if (user == null)
            return ApiResponse<UpdateAvatarResponseDTO>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        var storageService = _storageStrategy.GetStorageService(avatarFile.ContentType);
        var uploadResult = await storageService.UploadAsync(avatarFile, "avatars");

        if (!uploadResult.Success)
            return ApiResponse<UpdateAvatarResponseDTO>.Fail("UPLOAD_FAILED", "Tải ảnh lên thất bại.");
        var oldAvatarUrl = user.AvatarUrl;

        user.AvatarUrl = uploadResult.Url;
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            await storageService.DeleteAsync(uploadResult.Url);
            return ApiResponse<UpdateAvatarResponseDTO>.Fail("DB_UPDATE_FAILED", "Cập nhật ảnh đại diện thất bại.");
        }

        if (!string.IsNullOrEmpty(oldAvatarUrl))
        {
            await storageService.DeleteAsync(oldAvatarUrl);
        }

        var responseDto = new UpdateAvatarResponseDTO { NewAvatarUrl = uploadResult.Url };
        return ApiResponse<UpdateAvatarResponseDTO>.Ok(responseDto, "Cập nhật ảnh đại diện thành công.");
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordRequestDTO dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Người dùng không được xác thực.");

        var user = await _userManager.FindByIdAsync(currentUserId.ToString());
        if (user == null)
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (result.Succeeded)
        {
            return ApiResponse<object>.Ok(null, "Đổi mật khẩu thành công.");
        }

        var errors = result.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
        return ApiResponse<object>.Fail(errors);
    }

    public async Task<ApiResponse<List<LoginHistoryDto>>> GetMyLoginHistoryAsync()
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
        {
            return ApiResponse<List<LoginHistoryDto>>.Fail("UNAUTHORIZED", "Người dùng không hợp lệ.");
        }

        var history = await _unitOfWork.LoginHistories.GetQueryable()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.LoginTimestamp)
            .Take(10)
            .Select(h => new LoginHistoryDto
            {
                LoginTimestamp = h.LoginTimestamp,
                IpAddress = h.IpAddress,
                UserAgent = h.UserAgent,
                WasSuccessful = h.WasSuccessful
            })
            .ToListAsync();

        return ApiResponse<List<LoginHistoryDto>>.Ok(history, "Lấy lịch sử đăng nhập thành công.");
    }

    public async Task<ApiResponse<object>> RequestAccountDeletionAsync(DeleteAccountRequestDto dto)
    {
        var userId = Guid.Parse(_currentUser.Id!);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            return ApiResponse<object>.Fail("INVALID_PASSWORD", "Mật khẩu không chính xác.");

        user.IsActive = false;
        user.IsDeleted = true;
        await _userManager.UpdateAsync(user);
        return ApiResponse<object>.Ok(null, "Yêu cầu xóa tài khoản đã được tiếp nhận. Tài khoản của bạn sẽ bị xóa vĩnh viễn sau 30 ngày.");
    }

    public async Task<ApiResponse<object>> SubscribeToPushNotificationsAsync(Guid userId, string playerId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        user.OneSignalPlayerId = playerId;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return ApiResponse<object>.Fail("UPDATE_FAILED", "Cập nhật PlayerId thất bại.");

        return ApiResponse<object>.Ok(null, "Đăng ký nhận thông báo thành công.");
    }

    public async Task<ApiResponse<List<ContactDto>>> GetMyContactsAsync(Guid currentUserId)
    {
        try
        {
            var directConversationIds = await _unitOfWork.ConversationParticipants.GetQueryable()
                .Where(p => p.UserID == currentUserId && p.Conversation.ConversationType == EnumConversationType.Direct)
                .Select(p => p.ConversationID)
                .ToListAsync();
            var directChatPartnerIds = await _unitOfWork.ConversationParticipants.GetQueryable()
                .Where(p => directConversationIds.Contains(p.ConversationID) && p.UserID != currentUserId)
                .Select(p => p.UserID)
                .ToListAsync();

            var myGroupIds = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => gm.UserID == currentUserId)
                .Select(gm => gm.GroupID)
                .ToListAsync();
            var mutualGroupMemberIds = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => myGroupIds.Contains(gm.GroupID) && gm.UserID != currentUserId)
                .Select(gm => gm.UserID)
                .ToListAsync();

            var allContactIds = directChatPartnerIds
                .Union(mutualGroupMemberIds)
                .Distinct()
                .ToList();

            if (!allContactIds.Any())
            {
                return ApiResponse<List<ContactDto>>.Ok(new List<ContactDto>(), "Không tìm thấy liên hệ nào.");
            }

            var contacts = await _unitOfWork.Users.GetQueryable()
                .Where(u => allContactIds.Contains(u.Id) && !u.IsDeleted)
                .OrderBy(u => u.FullName)
                .Select(u => new ContactDto
                {
                    UserId = u.Id,
                    FullName = u.FullName!,
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync();

            var presenceStatuses = await _presenceService.GetStatusesForUsersAsync(allContactIds);
            foreach (var contact in contacts)
            {
                contact.PresenceStatus = presenceStatuses.GetValueOrDefault(contact.UserId, EnumUserPresenceStatus.Offline);
            }

            return ApiResponse<List<ContactDto>>.Ok(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh bạ cho người dùng {UserId}", currentUserId);
            return ApiResponse<List<ContactDto>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }
    public async Task<ApiResponse<PagedResult<UserSearchResultDTO>>> SearchUsersForInviteAsync(UserSearchForInviteRequest request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<PagedResult<UserSearchResultDTO>>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        try
        {
            var existingMemberIds = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => gm.GroupID == request.GroupId)
                .Select(gm => gm.UserID)
                .ToListAsync();

            var myDirectConvIds = await _unitOfWork.ConversationParticipants.GetQueryable()
                .Where(p => p.UserID == currentUserId && p.Conversation.ConversationType == EnumConversationType.Direct)
                .Select(p => p.ConversationID)
                .ToListAsync();
            var directChatPartnerIds = myDirectConvIds.Any()
                ? await _unitOfWork.ConversationParticipants.GetQueryable()
                    .Where(p => myDirectConvIds.Contains(p.ConversationID) && p.UserID != currentUserId)
                    .Select(p => p.UserID)
                    .ToListAsync()
                : new List<Guid>();

            var myGroupIds = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => gm.UserID == currentUserId)
                .Select(gm => gm.GroupID)
                .ToListAsync();
            var mutualGroupMemberIds = myGroupIds.Any()
                ? await _unitOfWork.GroupMembers.GetQueryable()
                    .Where(gm => myGroupIds.Contains(gm.GroupID) && gm.UserID != currentUserId)
                    .Select(gm => gm.UserID)
                    .Distinct()
                    .ToListAsync()
                : new List<Guid>();

            var baseQuery = _unitOfWork.Users.GetQueryable()
                .Where(u =>
                    u.Id != currentUserId &&
                    !existingMemberIds.Contains(u.Id) &&
                    (u.MessagingPrivacy == EnumMessagingPrivacy.FromAnyone ||
                     (u.MessagingPrivacy == EnumMessagingPrivacy.FromSharedGroupMembers &&
                      (directChatPartnerIds.Contains(u.Id) || mutualGroupMemberIds.Contains(u.Id))))
                );

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var term = request.Query.Trim();
                baseQuery = baseQuery.Where(u => (u.FullName != null && u.FullName.Contains(term)) ||
                                                 (u.Email != null && u.Email.Contains(term)));
            }

            var projectedQuery = baseQuery
                .Select(u => new
                {
                    User = u,
                    RelevanceScore = (directChatPartnerIds.Contains(u.Id) ? 100 : 0) +
                                     (mutualGroupMemberIds.Contains(u.Id) ? 10 : 0)
                });

            var sortedQuery = projectedQuery
                .OrderByDescending(x => x.RelevanceScore)
                .ThenBy(x => x.User.FullName);

            var pagedResult = await sortedQuery
                .Select(x => new UserSearchResultDTO
                {
                    UserId = x.User.Id,
                    DisplayName = x.User.FullName!,
                    Email = x.User.Email!,
                    AvatarUrl = x.User.AvatarUrl
                })
                .ToPagedResultAsync(request.PageNumber, request.PageSize);

            return ApiResponse<PagedResult<UserSearchResultDTO>>.Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm kiếm người dùng để mời vào nhóm {GroupId}", request.GroupId);
            return ApiResponse<PagedResult<UserSearchResultDTO>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    public async Task<ApiResponse<object>> DeactivateAccountAsync(DeactivateAccountDto dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
        {
            return ApiResponse<object>.Fail("INVALID_TOKEN", "User ID không hợp lệ.", 401);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.", 404);
        }

        var passwordCorrect = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
        if (!passwordCorrect)
        {
            return ApiResponse<object>.Fail("INVALID_PASSWORD", "Mật khẩu hiện tại không chính xác.");
        }

        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return ApiResponse<object>.Fail("DEACTIVATION_FAILED", "Không thể hủy kích hoạt tài khoản.");
        }

        await _unitOfWork.RefreshToken.RevokeAllForUserAsync(userId);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<object>.Ok(null, "Tài khoản đã được hủy kích hoạt. Bạn sẽ được đăng xuất.");
    }

    public async Task<ApiResponse<object>> UpdatePrivacySettingsAsync(UpdatePrivacySettingsDto dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
        {
            return ApiResponse<object>.Fail("INVALID_TOKEN", "User ID không hợp lệ.", 401);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.", 404);
        }

        user.MessagingPrivacy = dto.MessagingPrivacy;
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<object>.Ok(null, "Cập nhật cài đặt quyền riêng tư thành công.");
    }
    public async Task<ApiResponse<object>> DeactivateUserAccountAsync(Guid userId, string reason)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Attempted to deactivate a non-existent user: {UserId}", userId);
            return ApiResponse<object>.Ok(null, "Người dùng không tồn tại.");
        }

        if (!user.IsActive)
        {
            return ApiResponse<object>.Ok(null, "Tài khoản đã bị khóa trước đó.");
        }

        user.IsActive = false;
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            _logger.LogError("Failed to deactivate user {UserId}. Errors: {Errors}", userId, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            return ApiResponse<object>.Fail("DEACTIVATION_FAILED", "Không thể cập nhật trạng thái người dùng.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.RefreshToken.RevokeAllForUserAsync(userId);
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to revoke refresh tokens for deactivated user {UserId}", userId);
        }

        var eventData = new AccountDeactivatedEventData(reason);
        _backgroundJobClient.Enqueue<INotificationService>(service =>
            service.DispatchNotificationAsync<AccountDeactivatedNotificationTemplate, AccountDeactivatedEventData>(userId, eventData));

        _logger.LogInformation("Successfully deactivated account for user {UserId}. Reason: {Reason}", userId, reason);
        return ApiResponse<object>.Ok(null, "Vô hiệu hóa tài khoản thành công.");
    }

    public async Task<ApiResponse<List<MutualGroupDto>>> GetMutualGroupsAsync(Guid partnerUserId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<List<MutualGroupDto>>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        if (currentUserId == partnerUserId)
            return ApiResponse<List<MutualGroupDto>>.Ok(new List<MutualGroupDto>()); // Không có nhóm chung với chính mình

        try
        {
            var currentUserGroupIds = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => gm.UserID == currentUserId)
                .Select(gm => gm.GroupID)
                .ToListAsync();

            if (!currentUserGroupIds.Any())
                return ApiResponse<List<MutualGroupDto>>.Ok(new List<MutualGroupDto>());

            var mutualGroupsData = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => currentUserGroupIds.Contains(gm.GroupID) && 
                             (gm.UserID == currentUserId || gm.UserID == partnerUserId)) 
                .GroupBy(gm => gm.GroupID) // Nhóm theo từng Group
                .Where(g => g.Count() == 2) // Chỉ lấy những group mà cả 2 đều là thành viên
                .Select(g => new
                {
                    Group = g.First().Group, // Lấy thông tin Group
                    CurrentUserRole = g.FirstOrDefault(m => m.UserID == currentUserId)!.Role,
                    PartnerRole = g.FirstOrDefault(m => m.UserID == partnerUserId)!.Role
                })
                .ToListAsync();

            var resultDto = mutualGroupsData.Select(data => new MutualGroupDto
            {
                GroupId = data.Group.GroupID,
                GroupName = data.Group.GroupName,
                GroupAvatarUrl = data.Group.GroupAvatarUrl,
                CanKickPartner = (data.CurrentUserRole == EnumGroupRole.Admin && data.PartnerRole < EnumGroupRole.Admin) ||
                                 (data.CurrentUserRole == EnumGroupRole.Moderator && data.PartnerRole < EnumGroupRole.Moderator)
            }).ToList();

            return ApiResponse<List<MutualGroupDto>>.Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách nhóm chung giữa {CurrentUser} và {PartnerUser}", currentUserId, partnerUserId);
            return ApiResponse<List<MutualGroupDto>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    public async Task<ApiResponse<UserDashboardStatsDto>> GetDashboardStatsAsync()
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<UserDashboardStatsDto>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        try
        {
            // --- BƯỚC 1: Khởi tạo tác vụ đếm trên MongoDB ---
            // Tác vụ này có thể bắt đầu chạy ở chế độ nền
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var messagesTodayTask = _messageRepo.CountAsync(m =>
                m.Sender.UserId == userId &&
                m.SentAt >= today &&
                m.SentAt < tomorrow
            );

            // --- BƯỚC 2: Thực thi các truy vấn SQL một cách TUẦN TỰ ---

            // a. Đếm tổng số nhóm đã tham gia
            var joinedGroupsCount = await _unitOfWork.GroupMembers.GetQueryable()
                .CountAsync(gm => gm.UserID == userId && !gm.Group.IsDeleted);

            // b. Đếm số người đã chat 1-1
            var uniquePartnersCount = await _unitOfWork.ConversationParticipants.GetQueryable()
                .Where(p1 => p1.UserID == userId && p1.Conversation.ConversationType == EnumConversationType.Direct)
                .SelectMany(p1 => p1.Conversation.Participants.Where(p2 => p2.UserID != userId))
                .Select(p2 => p2.UserID)
                .Distinct()
                .CountAsync();

            // --- BƯỚC 3: Chờ tác vụ MongoDB hoàn tất và xây dựng DTO ---
            var messagesTodayCount = await messagesTodayTask;

            var stats = new UserDashboardStatsDto
            {
                MessagesTodayCount = messagesTodayCount,
                JoinedGroupsCount = joinedGroupsCount,
                UniqueDirectChatPartnersCount = uniquePartnersCount
            };

            return ApiResponse<UserDashboardStatsDto>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thống kê dashboard cho người dùng {UserId}", userId);
            return ApiResponse<UserDashboardStatsDto>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }
}
