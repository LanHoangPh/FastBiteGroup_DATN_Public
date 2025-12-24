using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.DTOs.Admin.User;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.Helper;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Infastructure.Caching;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Web;

namespace FastBiteGroupMCA.Infastructure.Services;

public class AdminUserService : IAdminUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AdminUserService> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly StorageStrategy _storageStrategy;
    private readonly IConfiguration _configuration;
    public AdminUserService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AdminUserService> logger, ICurrentUser currentUser, UserManager<AppUser> userManager, IEmailService emailService, IConfiguration configuration, IBackgroundJobClient backgroundJobClient, ICacheService cacheService, StorageStrategy storageStrategy, RoleManager<AppRole> roleManager)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _backgroundJobClient = backgroundJobClient;
        _cacheService = cacheService;
        _storageStrategy = storageStrategy;
        _roleManager = roleManager;
    }
    public async Task<ApiResponse<object>> DeactivateUserAccountAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, string reason, Guid? batchId = null)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            if (user.Id == adminId) return ApiResponse<object>.Fail("SELF_DEACTIVATION_NOT_ALLOWED", "Không thể tự vô hiệu hóa tài khoản của chính mình.");

            if (!user.IsActive)
                return ApiResponse<object>.Fail("USER_ALREADY_DEACTIVATED", "Tài khoản này đã ở trạng thái vô hiệu hóa.");

            var dbContext = _unitOfWork.GetDbContext();

            dbContext.Entry(user).Property("RowVersion").CurrentValue = rowVersion;

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return ApiResponse<object>.Fail("UPDATE_FAILED", "Vô hiệu hóa tài khoản thất bại.");


            await _unitOfWork.RefreshToken.RevokeUserTokensAsync(userId);

            await _userManager.UpdateSecurityStampAsync(user);

            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service =>
                service.LogAdminActionAsync(
                    adminId, adminFullName, EnumAdminActionType.UserDeactivated,
                    EnumTargetEntityType.User, userId.ToString(), $"Reason: {reason}",
                    batchId
                )
            );

            return ApiResponse<object>.Ok(null, "Tài khoản đã được vô hiệu hóa và tất cả phiên đăng nhập đã bị hủy.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<object>.Fail("CONCURRENCY_ERROR", "Dữ liệu vừa được thay đổi bởi người khác. Vui lòng làm mới trang và thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return ApiResponse<object>.Fail("DELETE_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
        
    }

    public async Task<ApiResponse<object>> ForcePasswordResetAsync(Guid userId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        // Logic tạo token và gửi email của bạn đã rất tốt, giữ nguyên
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var frontendBaseUrl = _configuration["AppUrls:FrontendBaseUrl"];
        var resetUrl = $"{frontendBaseUrl}/reset-password?token={HttpUtility.UrlEncode(token)}&email={user.Email}";

        await _emailService.SendAdminForcePasswordResetEmailAsync(user.Email!, user.FullName!, resetUrl);

        // BỔ SUNG: GHI LOG HÀNH ĐỘNG
        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId,
            _currentUser.FullName,
            EnumAdminActionType.UserPasswordResetForced, // Enum mới cho hành động này
            EnumTargetEntityType.User,
            userId.ToString(),
            $"Admin '{_currentUser.FullName}' đã buộc đặt lại mật khẩu cho người dùng '{user.UserName}'.",
            null
        ));

        return ApiResponse<object>.Ok(null, "Yêu cầu đặt lại mật khẩu đã được gửi thành công.");
    }

    public async Task<ApiResponse<object>> ReactivateUserAccountAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, Guid? batchId = null)
    {
        try
        {
            // Sử dụng IgnoreQueryFilters để có thể tìm thấy user kể cả khi họ đã bị xóa mềm
            var user = await _userManager.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

            if (user.IsActive)
                return ApiResponse<object>.Fail("USER_ALREADY_REACTIVE", "Tài khoản này đang ở trang thái hoạt động rồi");

            // BỔ SUNG: Kiểm tra nếu user đang ở trạng thái xóa mềm
            if (user.IsDeleted)
            {
                return ApiResponse<object>.Fail("USER_IS_DELETED", "Tài khoản này đã bị xóa. Vui lòng sử dụng chức năng 'Khôi phục' thay vì 'Kích hoạt'.");
            }
            var dbContext = _unitOfWork.GetDbContext();

            dbContext.Entry(user).Property("RowVersion").CurrentValue = rowVersion;

            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return ApiResponse<object>.Fail("UPDATE_FAILED", "Kích hoạt tài khoản thất bại.");

            // BỔ SUNG: GHI LOG HÀNH ĐỘNG KÍCH HOẠT
            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service =>
                service.LogAdminActionAsync(
                    adminId, adminFullName, EnumAdminActionType.UserReactivated, // Enum mới
                    EnumTargetEntityType.User, userId.ToString(), $"Admin đã kích hoạt lại tài khoản '{user.UserName}'.",
                    batchId
                )
            );

            return ApiResponse<object>.Ok(null, "Tài khoản đã được kích hoạt lại.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<object>.Fail("CONCURRENCY_ERROR", "Dữ liệu vừa được thay đổi bởi người khác. Vui lòng làm mới trang và thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return ApiResponse<object>.Fail("DELETE_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
        
    }
    public async Task<ApiResponse<AdminUserDetailDto>> GetUserDetailForAdminAsync(Guid userId)
    {
        // Sửa lỗi: Dùng _unitOfWork.Users và IgnoreQueryFilters để Admin có thể xem cả user đã bị xóa mềm
        var user = await _unitOfWork.Users.GetQueryable()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return ApiResponse<AdminUserDetailDto>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
        }

        // 1. Lấy danh sách nhóm
        var groupMemberships = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserID == userId && !gm.Group!.IsDeleted)
            .Select(gm => new UserGroupMembershipDto
            {
                GroupId = gm.GroupID,
                GroupName = gm.Group!.GroupName,
                UserRoleInGroup = gm.Role.ToString()
            })
            .ToListAsync(); // Hoàn thành truy vấn này trước

        // 2. Đếm số bài viết
        var postsCount = await _unitOfWork.Posts.GetQueryable()
            .CountAsync(p => p.AuthorUserID == userId && !p.IsDeleted); // Sau đó mới đến truy vấn này

        // 3. Lấy vai trò hệ thống
        var roles = await _userManager.GetRolesAsync(user); // Tiếp tục với truy vấn này

        // 4. Lấy 5 bài viết gần nhất
        var recentPosts = await _unitOfWork.Posts.GetQueryable()
            .Where(p => p.AuthorUserID == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new UserRecentPostDto
            {
                PostId = p.PostID,
                Title = p.Title,
                CreatedAt = p.CreatedAt,
                GroupName = p.Group!.GroupName,
                IsDeleted = p.IsDeleted
            })
            .ToListAsync(); // Cuối cùng là truy vấn này

        // --- Map vào DTO cuối cùng (Logic không đổi) ---
        var userDetailDto = new AdminUserDetailDto
        {
            UserId = user.Id.ToString(),
            FullName = user.FullName!,
            Email = user.Email!,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            IsDeleted = user.IsDeleted,
            RowVersion = user.RowVersion,
            Roles = roles.ToList(),
            Stats = new UserStatsDTO
            {
                GroupMembershipCount = groupMemberships.Count,
                TotalPostsCount = postsCount
            },
            GroupMemberships = groupMemberships,
            RecentPosts = recentPosts
        };

        return ApiResponse<AdminUserDetailDto>.Ok(userDetailDto, "Lấy chi tiết người dùng thành công.");
    }

    public async Task<ApiResponse<PagedResult<UserActivityDTO>>> GetUserActivityForAdminAsync(Guid userId, GetUserActivityParams     request)
    {
        var postsAsActivities = _unitOfWork.Posts.GetQueryable()
            .IgnoreQueryFilters()
            .Where(p => p.AuthorUserID == userId)
            .Where(p => !request.GroupId.HasValue || p.GroupID == request.GroupId.Value)
            .Select(p => new UserActivityRawDto
            {
                ActivityType = EnumUserActivityType.Post,
                Content = p.ContentJson,
                GroupName = p.Group!.GroupName,
                GroupId = p.GroupID,
                PostId = p.PostID,
                CommentId = (int?)null, // Để null cho các cột không áp dụng
                PostTitle = p.Title,
                CreatedAt = p.CreatedAt
            });

        var commentsAsActivities = _unitOfWork.PostComments.GetQueryable()
            .IgnoreQueryFilters()
            .Where(c => c.UserID == userId)
            .Where(c => !request.GroupId.HasValue || c.Post!.GroupID == request.GroupId.Value)
            .Select(c => new UserActivityRawDto
            {
                ActivityType = EnumUserActivityType.Comment,
                Content = c.Content,
                GroupName = c.Post!.Group!.GroupName,
                GroupId = c.Post!.GroupID,
                PostId = c.PostID,
                CommentId = (int?)c.CommentID,
                PostTitle = c.Post!.Title,
                CreatedAt = c.CreatedAt
            });

        var likesAsActivities = _unitOfWork.PostLikes.GetQueryable()
            .IgnoreQueryFilters()
            .Where(l => l.UserID == userId && !l.Post!.IsDeleted)
            .Where(l => !request.GroupId.HasValue || l.Post!.GroupID == request.GroupId.Value)
            .Select(l => new UserActivityRawDto
            {
                ActivityType = EnumUserActivityType.PostLike,
                Content = string.Empty, // Like không có content
                GroupName = l.Post!.Group!.GroupName,
                GroupId = l.Post!.GroupID,
                PostId = l.PostID,
                CommentId = (int?)null,
                PostTitle = l.Post!.Title,
                CreatedAt = l.CreatedAt
            });

        IQueryable<UserActivityRawDto>? combinedActivities = null;

        if (!request.ActivityType.HasValue || request.ActivityType == EnumUserActivityType.Post)
            combinedActivities = postsAsActivities;

        if (!request.ActivityType.HasValue || request.ActivityType == EnumUserActivityType.Comment)
            combinedActivities = (combinedActivities == null) ? commentsAsActivities : combinedActivities.Concat(commentsAsActivities);

        if (!request.ActivityType.HasValue || request.ActivityType == EnumUserActivityType.PostLike)
            combinedActivities = (combinedActivities == null) ? likesAsActivities : combinedActivities.Concat(likesAsActivities);

        if (combinedActivities == null)
        {
            return ApiResponse<PagedResult<UserActivityDTO>>.Ok(new PagedResult<UserActivityDTO>(new List<UserActivityDTO>(), 0, request.PageNumber, request.PageSize));
        }

        var finalQuery = combinedActivities.OrderByDescending(a => a.CreatedAt);

        var pagedRawResult = await finalQuery.ToPagedResultAsync(request.PageNumber, request.PageSize);

        var pagedItems = pagedRawResult.Items.Select(raw => new UserActivityDTO
        {
            ActivityType = raw.ActivityType,
            ContentPreview = raw.ActivityType == EnumUserActivityType.PostLike
                ? $"Đã thích bài viết: '{raw.PostTitle}'"
                : (raw.Content.Length > 150 ? raw.Content.Substring(0, 150) + "..." : raw.Content),
            GroupName = raw.GroupName,
            GroupId = raw.GroupId,
            PostId = raw.PostId,
            CreatedAt = raw.CreatedAt,
            Url = raw.ActivityType == EnumUserActivityType.Comment
                ? $"/groups/{raw.GroupId}/posts/{raw.PostId}#comment-{raw.CommentId}"
                : $"/groups/{raw.GroupId}/posts/{raw.PostId}"
        }).ToList();

        var pagedResult = new PagedResult<UserActivityDTO>(pagedItems, pagedRawResult.TotalRecords, pagedRawResult.PageNumber, pagedRawResult.PageSize);
        return ApiResponse<PagedResult<UserActivityDTO>>.Ok(pagedResult);
    }

    public async Task<ApiResponse<PagedResult<AdminUserListItemDTO>>> GetUsersForAdminAsync(GetUsersAdminParams request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentAdminId))
        {
            return ApiResponse<PagedResult<AdminUserListItemDTO>>.Fail("UNAUTHORIZED", "Không xác thực được Admin.");
        }
        var query = _userManager.Users.IgnoreQueryFilters().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = $"%{request.SearchTerm.Trim()}%";
            query = query.Where(u => (u.FullName != null && EF.Functions.Like(u.FullName, term)) ||
                                       (u.Email != null && EF.Functions.Like(u.Email, term)));
        }
        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }
        if (request.IsDeleted.HasValue)
        {
            query = query.Where(u => u.IsDeleted == request.IsDeleted.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = await _unitOfWork.Roles.GetQueryable()
                            .FirstOrDefaultAsync(r => r.Name == request.Role);
            if (role != null)
            {
                query = query.Where(u => _unitOfWork.UserRoles.GetQueryable()
                                            .Any(ur => ur.UserId == u.Id && ur.RoleId == role.Id));
            }
            else
            {
                return ApiResponse<PagedResult<AdminUserListItemDTO>>.Ok(
                    new PagedResult<AdminUserListItemDTO>(new List<AdminUserListItemDTO>(), 0, request.PageNumber, request.PageSize)
                );
            }
        }

        var sortedQuery = query
         .OrderByDescending(u => u.Id == currentAdminId) 
         .ThenByDescending(u => u.CreatedAt);             
        var pagedUsers = await sortedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize);
        var userIdsOnPage = pagedUsers.Items.Select(u => u.Id).ToList();

        var rolesLookup = (await (from userRole in _unitOfWork.UserRoles.GetQueryable()
                                  join role in _unitOfWork.Roles.GetQueryable() on userRole.RoleId equals role.Id
                                  where userIdsOnPage.Contains(userRole.UserId)
                                  select new { userRole.UserId, role.Name })
                                    .ToListAsync())
                                    .ToLookup(x => x.UserId, x => x.Name!);

        var userDtos = pagedUsers.Items.Select(u => new AdminUserListItemDTO
        {
            UserId = u.Id,
            FullName = u.FullName!,
            Email = u.Email!,
            AvatarUrl = u.AvatarUrl,
            Roles = rolesLookup.Contains(u.Id) ? rolesLookup[u.Id].ToList() : new List<string>(),
            DateOfBirth = u.DateOfBirth,
            CreatedAt = u.CreatedAt,
            IsActive = u.IsActive,
            IsDeleted = u.IsDeleted, 
            IsCurrentUser = u.Id == currentAdminId,
            RowVersion = u.RowVersion
        }).ToList();

        var finalPagedResult = new PagedResult<AdminUserListItemDTO>(
            userDtos, pagedUsers.TotalRecords, pagedUsers.PageNumber, pagedUsers.PageSize
        );

        return ApiResponse<PagedResult<AdminUserListItemDTO>>.Ok(finalPagedResult);
    }

    public async Task<ApiResponse<MyAdminProfileDto>> GetMyAdminProfileAsync()
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId))
            return ApiResponse<MyAdminProfileDto>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var adminUser = await _unitOfWork.Users.GetByIdAsync(adminId);
        if (adminUser == null)
            return ApiResponse<MyAdminProfileDto>.Fail("USER_NOT_FOUND", "Không tìm thấy tài khoản.");

        // Lấy lịch sử đăng nhập
        var loginHistory = await _unitOfWork.LoginHistories.GetQueryable()
            .Where(h => h.UserId == adminId)
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


        var recentActions = await _unitOfWork.AdminAuditLogs.GetQueryable()
        .Where(log => log.AdminUserId == adminId)
        .OrderByDescending(log => log.Timestamp)
        .Take(10)
        .Select(log => new AdminActionLogDto
        {
            ActionType = log.ActionType,
            TargetEntityType = log.TargetEntityType,
            TargetEntityId = log.TargetEntityId,
            Timestamp = log.Timestamp
        })
        .ToListAsync();

        var profileDto = new MyAdminProfileDto
        {
            UserId = adminUser.Id,
            FullName = adminUser.FullName!,
            Email = adminUser.Email!,
            AvatarUrl = adminUser.AvatarUrl,
            Bio = adminUser.Bio,
            DateOfBirth = adminUser.DateOfBirth,
            TwoFactorEnabled = adminUser.TwoFactorEnabled,
            CreatedAt = adminUser.CreatedAt,
            UpdateAt = adminUser.UpdatedAt ?? DateTime.MinValue,
            LastLogin = loginHistory.FirstOrDefault(),
            LoginHistory = loginHistory,
            RecentActions = recentActions
        };

        return ApiResponse<MyAdminProfileDto>.Ok(profileDto);
    }

    public async Task<ApiResponse<object>> RestoreUserAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, Guid? batchId = null)
    {
        try
        {
            var userToRestore = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

            if (userToRestore == null)
                return ApiResponse<object>.Fail("USER_NOT_FOUND", "Người dùng không tồn tại.");

            if (!userToRestore.IsDeleted) 
                return ApiResponse<object>.Fail("USER_NOT_DELETED", "Người dùng này không ở trạng thái bị xóa.");

            var dbContext = _unitOfWork.GetDbContext();

            dbContext.Entry(userToRestore).Property("RowVersion").CurrentValue = rowVersion;
            // Thực hiện khôi phục
            userToRestore.IsDeleted = false;
            var result = await _userManager.UpdateAsync(userToRestore);

            if (!result.Succeeded)
                return ApiResponse<object>.Fail("RESTORE_FAILED", "Khôi phục người dùng thất bại.");

            // Ghi log
            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                adminId, adminFullName,
                EnumAdminActionType.UserRestored, // Enum mới
                EnumTargetEntityType.User, userId.ToString(),
                $"Admin đã khôi phục tài khoản người dùng '{userToRestore.UserName}'.",
                batchId
            ));

            return ApiResponse<object>.Ok(null, "Khôi phục người dùng thành công.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<object>.Fail("CONCURRENCY_ERROR", "Dữ liệu vừa được thay đổi bởi người khác. Vui lòng làm mới trang và thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return ApiResponse<object>.Fail("DELETE_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
        
    }

    public async Task<ApiResponse<object>> DeleteUserAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, Guid? batchId = null)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            
            if(user.IsDeleted) return ApiResponse<object>.Fail("USER_IS_DELETED", "Người dùng này đã bị xóa từ trước.");

            var dbContext = _unitOfWork.GetDbContext();

            dbContext.Entry(user).Property("RowVersion").CurrentValue = rowVersion;

            user.IsDeleted = true;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
                return ApiResponse<object>.Fail(errors);
            }

            await _unitOfWork.RefreshToken.RevokeUserTokensAsync(userId);

            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                adminId,
                adminFullName,
                EnumAdminActionType.UserSoftDeleted, 
                EnumTargetEntityType.User,
                userId.ToString(),
                $"Admin '{_currentUser.FullName}' đã xóa mềm người dùng '{user.UserName}'.",
                batchId
            ));

            _logger.LogInformation("User {UserId} soft-deleted successfully by Admin {AdminId}", userId, adminId);
            return ApiResponse<object>.Ok(null, "Xóa người dùng thành công.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<object>.Fail("CONCURRENCY_ERROR", "Dữ liệu vừa được thay đổi bởi người khác. Vui lòng làm mới trang và thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return ApiResponse<object>.Fail("DELETE_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }
    public async Task<ApiResponse<object>> AssignRoleAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, string roleName, Guid? batchId = null)
    {
        try
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return ApiResponse<object>.Fail("ROLE_NOT_FOUND", "Vai trò không tồn tại trong hệ thống.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                return ApiResponse<object>.Fail("ROLE_ALREADY_ASSIGNED", "Người dùng đã có vai trò này.");
            }

            var dbContext = _unitOfWork.GetDbContext();

            dbContext.Entry(user).Property("RowVersion").CurrentValue = rowVersion;

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
                return ApiResponse<object>.Fail(errors);
            }

            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                adminId, adminFullName,
                EnumAdminActionType.UserRoleAssigned, 
                EnumTargetEntityType.User, userId.ToString(),
                $"Admin '{_currentUser.FullName}' đã gán vai trò '{roleName}' cho người dùng '{user.UserName}'.",
                batchId
            ));

            _logger.LogInformation("Role {RoleName} assigned to user {UserId}", roleName, userId);
            return ApiResponse<object>.Ok(null, $"Đã gán vai trò {roleName} cho người dùng."); 
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<object>.Fail("CONCURRENCY_ERROR", "Dữ liệu vừa được thay đổi bởi người khác. Vui lòng làm mới trang và thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return ApiResponse<object>.Fail("DELETE_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }

    public async Task<ApiResponse<object>> RemoveRoleAsync(Guid userId, byte[] rowVersion, string roleName)
    {
        try
        {
            // 1. Lấy thông tin admin thực hiện
            if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
            {
                return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
            }
            var adminFullName = _currentUser.FullName;
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                return ApiResponse<object>.Fail("USER_DOES_NOT_HAVE_ROLE", "Người dùng không có vai trò này.");
            }

            // BỔ SUNG: LỚP PHÒNG VỆ AN TOÀN - Ngăn chặn xóa Admin cuối cùng
            if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1 && admins.FirstOrDefault()?.Id == userId)
                {
                    return ApiResponse<object>.Fail("CANNOT_REMOVE_LAST_ADMIN", "Không thể xóa vai trò của người dùng Admin cuối cùng.");
                }
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
                return ApiResponse<object>.Fail(errors);
            }

            // BỔ SUNG: Ghi Log Kiểm toán
            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                adminId, adminFullName,
                EnumAdminActionType.UserRoleRemoved, // Enum mới
                EnumTargetEntityType.User, userId.ToString(),
                $"Admin '{_currentUser.FullName}' đã gỡ vai trò '{roleName}' khỏi người dùng '{user.UserName}'.",
                null
            ));

            _logger.LogInformation("Role {RoleName} removed from user {UserId}", roleName, userId);
            return ApiResponse<object>.Ok(null, $"Đã gỡ vai trò {roleName} khỏi người dùng.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
            return ApiResponse<object>.Fail("REMOVE_ROLE_ERROR", "Đã có lỗi xảy ra khi gỡ vai trò.");
        }
    }

    public async Task<ApiResponse<PagedResult<UserSearchResultDTO>>> SearchAvailableUsersAsync(UserSearchRequest request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
            return ApiResponse<PagedResult<UserSearchResultDTO>>.Fail("UNAUTHORIZED", "Người dùng không được xác thực.");

        var adminRoleId = await GetAdminRoleIdFromCacheAsync(); 
        if (adminRoleId == Guid.Empty)
            return ApiResponse<PagedResult<UserSearchResultDTO>>.Fail("ROLE_NOT_FOUND", "Không tìm thấy vai trò Admin.");

        var usersQuery = _unitOfWork.Users 
                                    .GetQueryable()
                                    .AsNoTracking();

        usersQuery = usersQuery.Where(u => u.Id != currentUserId);
        usersQuery = usersQuery.Where(u => !_unitOfWork.UserRoles.GetQueryable()
                                                .Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId));

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerm = $"%{request.Query.Trim()}%";
            usersQuery = usersQuery.Where(u =>
                EF.Functions.Like(u.UserName, searchTerm) ||
                EF.Functions.Like(u.Email, searchTerm) ||
                (u.FullName != null && EF.Functions.Like(u.FullName, searchTerm))
            );
        }

        if (request.ExcludeGroupId.HasValue)
        {
            usersQuery = usersQuery.Where(u => !_unitOfWork.GroupMembers.GetQueryable()
                                                .Any(gm => gm.GroupID == request.ExcludeGroupId.Value && gm.UserID == u.Id));
        }

        var projectedQuery = usersQuery
            .OrderBy(u => u.FullName)
            .Select(u => new UserSearchResultDTO
            {
                UserId = u.Id,
                DisplayName = u.FullName,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl
            });

        var pagedResult = await projectedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize);

        return ApiResponse<PagedResult<UserSearchResultDTO>>.Ok(pagedResult);
    }

    private async Task<Guid> GetAdminRoleIdFromCacheAsync()
    {
        const string cacheKey = "AdminRoleId";
        var cachedId = await _cacheService.GetAsync<Guid?>(cacheKey);
        if (cachedId.HasValue) return cachedId.Value;

        var adminRole = await _unitOfWork.Roles.GetQueryable().AsNoTracking().FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null) return Guid.Empty;

        await _cacheService.SetAsync(cacheKey, adminRole.Id, TimeSpan.FromHours(24));
        return adminRole.Id;
    }

    public async Task<ApiResponse<object>> CreateUserAsAdminAsync(CreateUserByAdminRequest request)
    {
        // 1. Lấy thông tin admin thực hiện
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }

        // 2. Kiểm tra Email/Username đã tồn tại
        if (await _userManager.FindByEmailAsync(request.Email) != null)
            return ApiResponse<object>.Fail("EMAIL_EXISTS", "Email đã tồn tại.");
        if (await _userManager.FindByNameAsync(request.UserName) != null)
            return ApiResponse<object>.Fail("USERNAME_EXISTS", "Username đã tồn tại.");

        // 3. Tạo đối tượng AppUser
        var newUser = new AppUser
        {
            Email = request.Email,
            UserName = request.UserName,
            FisrtName = request.FirstName,
            LastName = request.LastName,
            FullName = $"{request.FirstName} {request.LastName}",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Tạo mật khẩu ngẫu nhiên, an toàn
        var tempPassword = PasswordGenerator.GenerateRandomPassword(_userManager.Options.Password);

        // 5. Tạo user với mật khẩu tạm thời
        var createResult = await _userManager.CreateAsync(newUser, tempPassword);
        if (!createResult.Succeeded)
        {
            // Lấy lỗi từ Identity và trả về
            var errors = createResult.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
            return ApiResponse<object>.Fail(errors);
        }

        // 6. Gán vai trò cho user
        await _userManager.AddToRoleAsync(newUser, request.RoleName);

        var frontendBaseUrl = _configuration["AppUrls:FrontendBaseUrl"];

        var loginLink = $"{frontendBaseUrl}/login";

        // 7. Gửi Email chứa thông tin tài khoản
        await _emailService.SendTemporaryPasswordEmailAsync(newUser.Email, newUser.FullName, newUser.UserName, tempPassword, loginLink);

        // 8. Ghi log hành động của Admin
        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId, _currentUser.FullName,
            EnumAdminActionType.UserCreatedByAdmin, // Enum mới
            EnumTargetEntityType.User, newUser.Id.ToString(),
            $"Admin đã tạo tài khoản mới cho người dùng '{newUser.UserName}'.",
            null
        ));

        return ApiResponse<object>.Ok(null, "Tạo người dùng thành công. Email chứa thông tin đăng nhập đã được gửi đi.");
    }

    public async Task<ApiResponse<object>> UpdateUserBasicInfoAsync(Guid userId, UpdateUserBasicInfoRequest request)
    {
        try
        {
            if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
            {
                return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
            }
            var adminExecutor = _currentUser.FullName;

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

            var dbContext = _unitOfWork.GetDbContext();

            dbContext.Entry(user).Property("RowVersion").CurrentValue = request.RowVersion;

            user.FisrtName = request.FirstName;
            user.LastName = request.LastName;
            user.FullName = $"{request.FirstName} {request.LastName}";
            user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ApiResponse<object>.Fail("UPDATE_FAILED", "Cập nhật thông tin thất bại.");

            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                adminId, adminExecutor,
                EnumAdminActionType.UserUpdated,
                EnumTargetEntityType.User, userId.ToString(),
                $"Admin '{adminExecutor}' đã cập nhật thông tin cơ bản cho người dùng '{user.UserName}'.",
                null
            ));

            return ApiResponse<object>.Ok(null, "Cập nhật thông tin thành công.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiResponse<object>.Fail("CONCURRENCY_ERROR", "Dữ liệu vừa được thay đổi bởi người khác. Vui lòng làm mới trang và thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return ApiResponse<object>.Fail("DELETE_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
        
    }

    public async Task<ApiResponse<object>> RemoveUserAvatarAsync(Guid userId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }
        var adminExecutor = _currentUser.FullName;
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        var oldAvatarUrl = user.AvatarUrl;
        if (string.IsNullOrEmpty(oldAvatarUrl))
            return ApiResponse<object>.Ok(null, "Người dùng không có ảnh đại diện để xóa.");

        user.AvatarUrl = null;
        await _userManager.UpdateAsync(user);

        try
        {
            await _storageStrategy.GetStorageService("image/jpeg").DeleteAsync(oldAvatarUrl); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa file avatar cũ: {Url}", oldAvatarUrl);
        }

        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId, adminExecutor,
            EnumAdminActionType.UserAvatarRemoved,
            EnumTargetEntityType.User, userId.ToString(),
            $"Admin '{adminExecutor}' đã xóa ảnh đại diện của người dùng '{user.UserName}'.",
            null
        ));

        return ApiResponse<object>.Ok(null, "Xóa ảnh đại diện thành công.");
    }

    public async Task<ApiResponse<object>> RemoveUserBioAsync(Guid userId)
    {
        // 1. Lấy thông tin admin thực hiện
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }
        var adminExecutor = _currentUser.FullName;
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        user.Bio = null;
        await _userManager.UpdateAsync(user);

        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId, adminExecutor,
            EnumAdminActionType.UserBioRemoved, // Enum mới
            EnumTargetEntityType.User, userId.ToString(),
            $"Admin '{adminExecutor}' đã xóa tiểu sử của người dùng '{user.UserName}'.",
            null
        ));

        return ApiResponse<object>.Ok(null, "Xóa tiểu sử thành công.");
    }

    public ApiResponse<object> PerformBulkUserActionAsync(BulkUserActionRequest request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }
        // 1. Xác thực tham số phụ dựa trên hành động
        if ((request.Action == EnumBulkUserActionType.AssignRole) && string.IsNullOrWhiteSpace(request.RoleName))
        {
            return ApiResponse<object>.Fail("ROLE_NAME_REQUIRED", "Vui lòng cung cấp tên vai trò.");
        }
        if (request.Action == EnumBulkUserActionType.Deactivate && string.IsNullOrWhiteSpace(request.ReasonDetails) && string.IsNullOrWhiteSpace(request.ReasonCategory) )
        {
            return ApiResponse<object>.Fail("REASON_REQUIRED", "Vui lòng cung cấp lý do vô hiệu hóa.");
        }
        var fullReason = $"{request.ReasonCategory}: {request.ReasonDetails ?? "N/A"}";
        var adminFullName = _currentUser.FullName;
        var batchId = Guid.NewGuid();
        // 2. Lặp qua danh sách UserIds và xếp hàng các công việc
        // SỬA LẠI: Lặp qua request.Users thay vì request.UserIds
        foreach (var userWithVersion in request.Users.DistinctBy(u => u.UserId)) // Dùng DistinctBy để tránh xử lý trùng UserId
        {
            // Lấy UserId và RowVersion từ mỗi đối tượng trong danh sách
            var userId = userWithVersion.UserId;
            var rowVersion = userWithVersion.RowVersion;

            switch (request.Action)
            {
                case EnumBulkUserActionType.Activate:
                    // Truyền RowVersion của từng user vào job tương ứng
                    _backgroundJobClient.Enqueue(() => ReactivateUserAccountAsync(userId, rowVersion, adminId, adminFullName, batchId));
                    break;
                case EnumBulkUserActionType.Deactivate:
                    _backgroundJobClient.Enqueue(() => DeactivateUserAccountAsync(userId, rowVersion, adminId, adminFullName, fullReason, batchId));
                    break;
                case EnumBulkUserActionType.SoftDelete:
                    _backgroundJobClient.Enqueue(() => DeleteUserAsync(userId, rowVersion, adminId, adminFullName, batchId));
                    break;
                case EnumBulkUserActionType.Restore:
                    _backgroundJobClient.Enqueue(() => RestoreUserAsync(userId, rowVersion, adminId, adminFullName, batchId));
                    break;
                case EnumBulkUserActionType.AssignRole:
                    _backgroundJobClient.Enqueue(() => AssignRoleAsync(userId, rowVersion, adminId, adminFullName, request.RoleName, batchId));
                    break;
            }
        }

        // --- Phần 3: Xếp hàng Job Thông báo Hoàn tất (Đã được cập nhật) ---

        // Sửa lại để lấy Count từ danh sách mới
        var totalJobs = request.Users.Count;

        var finalJobId = _backgroundJobClient.Schedule<IAdminNotificationService>(
            service => service.SendBulkActionCompletionNotificationAsync(
                adminId,
                batchId,
                totalJobs, // Sử dụng số lượng đúng
                request.Action.ToString()
            ),
            TimeSpan.FromSeconds(15) // Delay một khoảng thời gian hợp lý
        );

        // Trả về phản hồi ngay lập tức
        return ApiResponse<object>.Ok(null, $"Yêu cầu xử lý hàng loạt cho {totalJobs} người dùng đã được tiếp nhận.");
    }
}
internal class UserActivityRawDto
{
    public EnumUserActivityType ActivityType { get; set; } 
    public string Content { get; set; } = string.Empty;
    public string? PostTitle { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public int PostId { get; set; }
    public int? CommentId { get; set; }
    public DateTime CreatedAt { get; set; }
}
