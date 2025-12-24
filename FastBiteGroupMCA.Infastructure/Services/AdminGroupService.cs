using FastBiteGroupMCA.Application.DTOs.Admin.Group;
using FastBiteGroupMCA.Application.DTOs.Admin.Post;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Ganss.Xss;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics.SymbolStore;

namespace FastBiteGroupMCA.Infastructure.Services;

public class AdminGroupService : IAdminGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminGroupService> _logger;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly IBackgroundJobClient _backgroundJobClient; 
    private readonly StorageStrategy _storageStrategy;
    private readonly ISettingsService _settingsService;
    public AdminGroupService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AdminGroupService> logger, ICurrentUser currentUser, IBackgroundJobClient backgroundJobClient, StorageStrategy storageStrategy, ISettingsService settingsService, UserManager<AppUser> userManager, IHtmlSanitizer htmlSanitizer)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _currentUser = currentUser;
        _storageStrategy = storageStrategy;
        _settingsService = settingsService;
        _userManager = userManager;
        _htmlSanitizer = htmlSanitizer;
    }
    public async Task<ApiResponse<AdminGroupDetailDTO>> GetGroupDetailsAsync(Guid groupId)
    {
        var groupDetails = await _unitOfWork.Groups.GetQueryable()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(g => g.GroupID == groupId)
            .Include(g => g.CreatedByUser)
            .Include(g => g.Posts)
            .Include(g => g.ContentReports)
            .Select(g => new AdminGroupDetailDTO
            {
                GroupId = g.GroupID,
                GroupName = g.GroupName,
                Description = g.Description,
                GroupAvatarUrl = g.GroupAvatarUrl,
                CreatorName = g.CreatedByUser!.FullName,
                GroupType = g.GroupType,
                CreatedAt = g.CreatedAt,
                IsArchived = g.IsArchived,
                IsDeleted = g.IsDeleted, 
                Stats = new GroupStatsDTO
                {
                    MemberCount = g.Members.Count(),
                    PostCount = g.Posts.Count(p => !p.IsDeleted),
                    PendingReportsCount = g.ContentReports.Count(r => r.Status == EnumContentReportStatus.Pending),
                    LastActivityAt = g.Posts.Any() ? g.Posts.Max(p => p.CreatedAt) : g.CreatedAt
                },
            })
            .FirstOrDefaultAsync();

        if (groupDetails == null)
            return ApiResponse<AdminGroupDetailDTO>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");

        return ApiResponse<AdminGroupDetailDTO>.Ok(groupDetails);
    }

    // API 2: Quản lý thành viên
    public async Task<ApiResponse<PagedResult<GroupAdminMemberDTO>>> GetGroupMembersAsync(Guid groupId, GetGroupMembersParams request)
    {
        var membersQuery = _unitOfWork.GroupMembers.GetQueryable()
        .AsNoTracking()
        .Where(gm => gm.GroupID == groupId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower().Trim();
            membersQuery = membersQuery.Include(gm => gm.User)
                                       .Where(gm => gm.User!.FullName!.ToLower().Contains(term));
        }

        var orderedQuery = membersQuery
            .OrderByDescending(gm => gm.Role)
            .ThenByDescending(gm => gm.JoinedAt);

        var pagedResult = await membersQuery
            .Select(gm => new GroupAdminMemberDTO
            {
                UserId = gm.UserID,
                FullName = gm.User!.FullName!,
                AvatarUrl = gm.User.AvatarUrl,
                Role = gm.Role,
                JoinedAt = gm.JoinedAt
            })
            .ToPagedResultAsync(request.PageNumber, request.PageSize);

        return ApiResponse<PagedResult<GroupAdminMemberDTO>>.Ok(pagedResult);
    }

    public async Task<ApiResponse<object>> UpdateMemberRoleAsync(Guid groupId, Guid userIdToUpdate, EnumGroupRole newRole)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được người thực hiện.");
        }

        try
        {
            var group = await _unitOfWork.Groups.GetQueryable()
                .Include(g => g.Members) 
                .FirstOrDefaultAsync(g => g.GroupID == groupId);

            if (group == null)
                return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Nhóm không tồn tại.");

            var adminExecutor = group.Members.FirstOrDefault(m => m.UserID == adminId);

            var memberToUpdate = group.Members.FirstOrDefault(m => m.UserID == userIdToUpdate);
            if (memberToUpdate == null)
                return ApiResponse<object>.Fail("MEMBER_NOT_FOUND", "Thành viên không tồn tại trong nhóm.");

            if (adminId == userIdToUpdate)
            {
                return ApiResponse<object>.Fail("SELF_UPDATE_NOT_ALLOWED", "Không thể tự thay đổi vai trò của chính mình.");
            }

            if (memberToUpdate.Role == EnumGroupRole.Admin && newRole != EnumGroupRole.Admin)
            {
                if (group.Members.Count(m => m.Role == EnumGroupRole.Admin) <= 1)
                {
                    return ApiResponse<object>.Fail("CANNOT_DEMOTE_LAST_ADMIN", "Không thể hạ vai trò của Admin cuối cùng.");
                }
            }

            memberToUpdate.Role = newRole;
            _unitOfWork.GroupMembers.Update(memberToUpdate); 

            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                    adminId,
                    _currentUser.FullName,
                    EnumAdminActionType.GroupMemberRoleChanged, 
                    EnumTargetEntityType.GroupMember,
                    memberToUpdate.GroupMemberID.ToString(),
                    $"Admin đã cập nhật vai trò của thành viên (UserID: {userIdToUpdate}) trong nhóm '{group.GroupName}' thành '{newRole}'.",
                    null
                ));


                return ApiResponse<object>.Ok(null, "Cập nhật vai trò thành công.");
            }

            return ApiResponse<object>.Fail("SAVE_CHANGES_FAILED", "Cập nhật vai trò thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật vai trò cho thành viên {UserId} trong nhóm {GroupId}", userIdToUpdate, groupId);
            return ApiResponse<object>.Fail("UPDATE_ROLE_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }

    public async Task<ApiResponse<object>> RemoveMemberAsync(Guid groupId, Guid userIdToRemove)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }

        if (adminId == userIdToRemove)
        {
            return ApiResponse<object>.Fail("CANNOT_REMOVE_SELF", "Admin không thể tự xóa chính mình.");
        }

        try
        {
            var group = await _unitOfWork.Groups.GetQueryable()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupID == groupId);

            if (group == null)
                return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Nhóm không tồn tại.");

            var memberToRemove = group.Members.FirstOrDefault(m => m.UserID == userIdToRemove);

            if (memberToRemove == null)
                return ApiResponse<object>.Fail("MEMBER_NOT_FOUND", "Thành viên không tồn tại trong nhóm.");

            if (group.CreatedByUserID == userIdToRemove)
            {
                return ApiResponse<object>.Fail("CANNOT_REMOVE_CREATOR", "Không thể xóa người sáng lập nhóm.");
            }
            if (memberToRemove.Role == EnumGroupRole.Admin && group.Members.Count(m => m.Role == EnumGroupRole.Admin) <= 1)
            {
                return ApiResponse<object>.Fail("CANNOT_REMOVE_LAST_ADMIN", "Không thể xóa Admin cuối cùng của nhóm.");
            }

            var conversation = await _unitOfWork.Conversations.GetQueryable()
                .FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);

            if (conversation != null)
            {
                var participantToRemove = await _unitOfWork.ConversationParticipants.GetQueryable()
                    .FirstOrDefaultAsync(p => p.ConversationID == conversation.ConversationID && p.UserID == userIdToRemove);

                if (participantToRemove != null)
                {
                    _unitOfWork.ConversationParticipants.Remove(participantToRemove);
                }
            }
            else
            {
                _logger.LogWarning("Group {GroupId} is missing an associated conversation.", groupId);
            }

            _unitOfWork.GroupMembers.Remove(memberToRemove);

            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                    adminId,
                    _currentUser.FullName,
                    EnumAdminActionType.GroupMemberRemoved, 
                    EnumTargetEntityType.GroupMember,
                    memberToRemove.GroupMemberID.ToString(), 
                    $"Admin đã xóa thành viên (UserID: {userIdToRemove}) khỏi nhóm '{group.GroupName}' (GroupID: {groupId}).",
                    null
                ));

                return ApiResponse<object>.Ok(null, "Xóa thành viên thành công.");
            }

            return ApiResponse<object>.Fail("SAVE_CHANGES_FAILED", "Xóa thành viên thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa thành viên {UserId} khỏi nhóm {GroupId}", userIdToRemove, groupId);
            return ApiResponse<object>.Fail("REMOVE_MEMBER_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }

    // API 3: Quản lý nội dung
    public async Task<ApiResponse<PagedResult<PostForListDTO>>> GetGroupPostsAsync(Guid groupId, GetGroupPostsParams request)
    {
        var postsQuery = _unitOfWork.Posts.GetQueryable()
            .IgnoreQueryFilters()
            .Where(p => p.GroupID == groupId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower().Trim();
            postsQuery = postsQuery.Where(p => p.Title!.ToLower().Contains(term) || p.ContentJson.ToLower().Contains(term));
        }

        var pagedResult = await postsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostForListDTO
            {
                PostId = p.PostID,
                Title = p.Title,
                AuthorId = p.AuthorUserID,
                AuthorName = p.Author!.FullName!,
                AuthorAvatarUrl = p.Author.AvatarUrl,
                CreatedAt = p.CreatedAt,
                LikeCount = p.Likes.Count(),
                CommentCount = p.Comments.Count(),
                IsPinned = p.IsPinned,
                IsDeleted = p.IsDeleted
            })
            .ToPagedResultAsync(request.PageNumber, request.PageSize);

        return ApiResponse<PagedResult<PostForListDTO>>.Ok(pagedResult);
    }

    public async Task<ApiResponse<object>> DeletePostAsync(int postId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }
        var post = await _unitOfWork.Posts.GetByIdAsync(postId);
        if (post == null)
            return ApiResponse<object>.Fail("POST_NOT_FOUND", "Không tìm thấy bài đăng.");

        post.IsDeleted = true; 
        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                adminId,
                _currentUser.FullName,
                EnumAdminActionType.PostSoftDeleted, 
                EnumTargetEntityType.Post,                             
                postId.ToString(),
                $"Admin đã xóa mềm bài viết có tiêu đề: '{post.Title}'.",
                null
            ));
        return ApiResponse<object>.Ok(null, "Xóa bài đăng thành công.");
    }

    public async Task<ApiResponse<bool>> SoftDeleteGroupAsync(Guid groupId, Guid adminId, string adminFullName, Guid? batchId = null)
    {
        var strategy = _unitOfWork.GetDbContext().Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Tìm nhóm
                var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
                if (group is null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResponse<bool>.Fail("GROUP_NOT_FOUND",
                                                  "Không tìm thấy nhóm với ID đã cung cấp.");
                }

                if (group.IsDeleted)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResponse<bool>.Ok(true, "Nhóm đã bị xoá trước đó.");
                }

                group.IsDeleted = true;
                _unitOfWork.Groups.Update(group);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Group {GroupId} soft‑deleted by user {UserId}",
                                       groupId, _currentUser.Id);

                _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                    adminId,
                    adminFullName,
                    EnumAdminActionType.GroupSoftDeleted,
                    EnumTargetEntityType.Group,
                    groupId.ToString(),
                    $"Admin đã Xóa mềm nhóm '{group.GroupName}'.",
                    batchId
                ));

                return ApiResponse<bool>.Ok(true, "Xoá nhóm thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error soft‑deleting group {GroupId}", groupId);
                throw;         
            }
        });
    }

    public async Task<ApiResponse<object>> UpdateGroupInfoAsync(Guid groupId, UpdateGroupRequestDTO request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }

        try
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null)
            {
                return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");
            }

            var isGroupNameTaken = await _unitOfWork.Groups.GetQueryable()
                .AnyAsync(g => g.GroupName == request.GroupName && g.GroupID != groupId);

            if (isGroupNameTaken)
            {
                return ApiResponse<object>.Fail("GROUP_NAME_TAKEN", "Tên nhóm đã tồn tại.");
            }

            group.GroupName = request.GroupName;
            group.Description = request.Description;
            group.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Groups.Update(group);
            var result = await _unitOfWork.SaveChangesAsync();

            if (result > 0)
            {
                _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                    adminId,
                    _currentUser.FullName,
                    EnumAdminActionType.GroupUpdated,
                    EnumTargetEntityType.Group,
                    groupId.ToString(),
                    $"Admin đã cập nhật tên/mô tả cho nhóm '{group.GroupName}'.",
                    null
                ));

                return ApiResponse<object>.Ok(null, "Cập nhật thông tin nhóm thành công.");
            }

            return ApiResponse<object>.Fail("SAVE_CHANGES_FAILED", "Cập nhật nhóm thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi admin cập nhật thông tin nhóm {GroupId}", groupId);
            return ApiResponse<object>.Fail("UPDATE_GROUP_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }
    public async Task<ApiResponse<PagedResult<GroupForList_AdminDto>>> GetGroupsForAdminAsync(GetGroupsAdminParams request)
    {
        try
        {
            var query = _unitOfWork.Groups.GetQueryable()
                .IgnoreQueryFilters()
                .AsNoTracking();

            switch (request.Status)
            {
                case GroupStatusFilter.Archived:
                    query = query.Where(g => g.IsArchived && !g.IsDeleted);
                    break;

                case GroupStatusFilter.Deleted:
                    query = query.Where(g => g.IsDeleted);
                    break;

                case GroupStatusFilter.Active:
                    query = query.Where(g => !g.IsArchived && !g.IsDeleted);
                    break;

                // BỔ SUNG CASE MỚI
                case GroupStatusFilter.All:
                    // Không cần làm gì cả, chúng ta lấy tất cả group
                    // không phân biệt trạng thái IsArchived hay IsDeleted.
                    break;

                default:
                    // Mặc định vẫn là Active
                    query = query.Where(g => !g.IsArchived && !g.IsDeleted);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(g => g.GroupName.ToLower().Contains(request.SearchTerm.Trim().ToLower()));
            }
            if (request.GroupType.HasValue)
            {
                query = request.GroupType.Value switch
                {
                    MyGroupFilterType.Chat => query.Where(g => g.GroupType == EnumGroupType.Public || g.GroupType == EnumGroupType.Private),
                    MyGroupFilterType.Community => query.Where(g => g.GroupType == EnumGroupType.Community),
                    _ => query // All hoặc các trường hợp khác
                };
            }

            var projectedQuery = query.Select(g => new GroupForList_AdminDto
            {
                GroupId = g.GroupID,
                GroupName = g.GroupName,
                CreatorName = g.CreatedByUser!.FullName!,
                MemberCount = g.Members.Count(),
                PostCount = g.Posts.Count(p => !p.IsDeleted),
                GroupType = (g.GroupType == EnumGroupType.Community
                    ? GroupTypeApiDto.Community
                    : GroupTypeApiDto.Chat),
                IsArchived = g.IsArchived,
                Privacy = g.Privacy,
                IsDeleted = g.IsDeleted,
                CreatedAt = g.CreatedAt,
                LastActivityAt = g.Posts.Any()
                ? g.Posts.OrderByDescending(p => p.CreatedAt).Select(p => p.CreatedAt).FirstOrDefault()
                : g.CreatedAt,
                PendingReportsCount = g.ContentReports.Count(r => r.Status == EnumContentReportStatus.Pending)
            });

            projectedQuery = projectedQuery.OrderByDescending(g => g.LastActivityAt);

            var pagedResult = await projectedQuery.ToPagedResultAsync(request.PageNumber, request.PageSize);

            return ApiResponse<PagedResult<GroupForList_AdminDto>>.Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups list for admin");
            return ApiResponse<PagedResult<GroupForList_AdminDto>>.Fail("GET_GROUPS_ERROR", "Đã có lỗi xảy ra khi lấy danh sách nhóm.");
        }
    }
    public async Task<ApiResponse<object>> UpdateGroupSettingsAsync(Guid groupId, UpdateGroupSettingsDTO dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }
        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
            return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");

        group.GroupType = dto.GroupType;

        _unitOfWork.Groups.Update(group);
        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId,
            _currentUser.FullName,
            EnumAdminActionType.GroupSettingsChanged,
            EnumTargetEntityType.Group,
            groupId.ToString(),
            $"Admin đã CẬP NHẬT cài đặt nhóm '{group.GroupName}' thành công.",
            null
        ));

        return ApiResponse<object>.Ok(null, "Cập nhật loại nhóm thành công.");
    }
    public async Task<ApiResponse<object>> ChangeGroupOwnerAsync(Guid groupId, Guid newOwnerUserId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null)
                return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");

            var currentOwnerId = group.CreatedByUserID;
            if (currentOwnerId == newOwnerUserId)
                return ApiResponse<object>.Fail("SAME_OWNER", "Người dùng này đã là chủ sở hữu của nhóm.");

            var newOwnerMembership = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == newOwnerUserId);

            if (newOwnerMembership == null)
            {
                await transaction.RollbackAsync();
                return ApiResponse<object>.Fail("USER_NOT_IN_GROUP", "Người dùng được chọn không phải là thành viên của nhóm.");
            }

            group.CreatedByUserID = newOwnerUserId;

            newOwnerMembership.Role = EnumGroupRole.Admin;

            var oldOwnerMembership = await _unitOfWork.GroupMembers.GetQueryable()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == currentOwnerId);

            if (oldOwnerMembership != null)
            {
                oldOwnerMembership.Role = EnumGroupRole.Admin; 
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                    adminId,
                    _currentUser.FullName,
                    EnumAdminActionType.GroupOwnerChanged,
                    EnumTargetEntityType.Group,
                    groupId.ToString(),
                    $"Admin đã ĐỔI QUẢN TRỊ VIÊN nhóm '{group.GroupName}'.",
                    null
                ));

            return ApiResponse<object>.Ok(null, "Chuyển quyền sở hữu nhóm thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error changing owner for group {GroupId}", groupId); 
            return ApiResponse<object>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    public async Task<ApiResponse<bool>> ArchiveGroupAsAdminAsync(Guid groupId, Guid adminId, string adminFullname, Guid? batchId = null)
    {
        try
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null) return ApiResponse<bool>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");
            if (group.IsDeleted) return ApiResponse<bool>.Fail("GROUP_ALREADY_DELETED", "Không thể lưu trữ một nhóm đã bị xóa.");
            if (group.IsArchived) return ApiResponse<bool>.Ok(true, "Nhóm đã ở trạng thái lưu trữ.");

            group.IsArchived = true;
            _unitOfWork.Groups.Update(group);

            var result = await _unitOfWork.SaveChangesAsync();
            if (result > 0)
            {
                _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                    adminId,
                    adminFullname,
                    EnumAdminActionType.GroupArchived,
                    EnumTargetEntityType.Group,
                    groupId.ToString(),
                    $"Admin đã LƯU TRỮ nhóm '{group.GroupName}'.",
                    batchId
                ));
                return ApiResponse<bool>.Ok(true, "Lưu trữ nhóm thành công.");
            }
            return ApiResponse<bool>.Fail("SAVE_CHANGES_FAILED", "Lưu thay đổi thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi admin lưu trữ nhóm {GroupId}", groupId);
            return ApiResponse<bool>.Fail("ARCHIVE_GROUP_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    public async Task<ApiResponse<bool>> UnarchiveGroupAsAdminAsync(Guid groupId, Guid adminId, string admimFullName, Guid? batchId = null)
    {
        try
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null) return ApiResponse<bool>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");
            if (!group.IsArchived) return ApiResponse<bool>.Ok(true, "Nhóm không ở trạng thái lưu trữ.");

            group.IsArchived = false;
            _unitOfWork.Groups.Update(group);

            var result = await _unitOfWork.SaveChangesAsync();
            if (result > 0)
            {
                _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                    adminId,
                    admimFullName,
                    EnumAdminActionType.GroupUnarchived, 
                    EnumTargetEntityType.Group,
                    groupId.ToString(),
                    $"Admin đã BỎ LƯU TRỮ nhóm '{group.GroupName}'.",
                    batchId
                ));
                return ApiResponse<bool>.Ok(true, "Bỏ lưu trữ nhóm thành công.");
            }
            return ApiResponse<bool>.Fail("SAVE_CHANGES_FAILED", "Lưu thay đổi thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi admin bỏ lưu trữ nhóm {GroupId}", groupId);
            return ApiResponse<bool>.Fail("UNARCHIVE_GROUP_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    public async Task<ApiResponse<object>> AddMemberAsSystemAdminAsync(Guid groupId, AddMemberAdminRequest request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
            return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Nhóm không tồn tại.");

        var userToAdd = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (userToAdd == null)
            return ApiResponse<object>.Fail("USER_NOT_FOUND", "Người dùng không tồn tại.");

        var isAlreadyMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(m => m.GroupID == groupId && m.UserID == request.UserId);

        if (isAlreadyMember)
        {
            return ApiResponse<object>.Fail("MEMBER_ALREADY_EXISTS", "Người dùng đã là thành viên của nhóm.");
        }

        var newMember = new GroupMember
        {
            GroupID = groupId,
            UserID = request.UserId,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow
        };
        await _unitOfWork.GroupMembers.AddAsync(newMember);

        var conversation = await _unitOfWork.Conversations.GetQueryable()
            .FirstOrDefaultAsync(c => c.ExplicitGroupID == groupId);

        if (conversation != null)
        {
            var newParticipant = new ConversationParticipants
            {
                ConversationID = conversation.ConversationID,
                UserID = request.UserId,
                JoinedAt = DateTime.UtcNow
            };
            await _unitOfWork.ConversationParticipants.AddAsync(newParticipant);
        }
        else
        {
            _logger.LogWarning("Group {GroupId} is missing an associated conversation when adding member.", groupId);
        }

        await _unitOfWork.SaveChangesAsync();

        var groupName = group.GroupName; 
        var userToAddName = userToAdd.FullName; 
        var systemAdminName = _currentUser.FullName;

        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId,
            _currentUser.FullName,
            EnumAdminActionType.GroupMemberAddedBySystem, 
            EnumTargetEntityType.GroupMember,
            groupId.ToString(),
            $"Admin tổng đã thêm người dùng '{userToAdd.FullName}' (ID: {request.UserId}) vào nhóm.",
            null
        ));

        _backgroundJobClient.Enqueue<INotificationService>(service =>
            service.NotifyUserAddedToGroupByAdminAsync(
                request.UserId,
                userToAddName,
                groupId,
                groupName,
                systemAdminName
            ));

        return ApiResponse<object>.Ok(null, "Thêm thành viên vào nhóm thành công.");
    }

    public async Task<ApiResponse<object>> RestoreGroupAsAdminAsync(Guid groupId, Guid adminId, string adminFullName, Guid? batchId = null)
    {
        var group = await _unitOfWork.Groups.GetByIdGroupAsync(groupId, ignoreQueryFilters: true);

        if (group == null)
        {
            return ApiResponse<object>.Fail("GROUP_NOT_FOUND", "Nhóm không tồn tại.");
        }

        if (!group.IsDeleted)
        {
            return ApiResponse<object>.Fail("GROUP_NOT_DELETED", "Nhóm này không ở trong trạng thái bị xóa.");
        }

        var isNameTaken = await _unitOfWork.Groups.GetQueryable()
            .AnyAsync(g => g.GroupName == group.GroupName && !g.IsDeleted);

        if (isNameTaken)
        {
            return ApiResponse<object>.Fail("GROUP_NAME_CONFLICT", $"Không thể khôi phục. Đã có một nhóm khác đang hoạt động với tên '{group.GroupName}'.");
        }

        group.IsDeleted = false;
        group.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Groups.Update(group);

        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId,
            adminFullName,
            EnumAdminActionType.GroupRestored,
            EnumTargetEntityType.Group,
            groupId.ToString(),
            $"Admin đã khôi phục nhóm '{group.GroupName}'.",
            batchId
        ));

        return ApiResponse<object>.Ok(null, "Khôi phục nhóm thành công.");
    }

    public async Task<ApiResponse<string>> UpdateGroupAvatarAsAdminAsync(Guid groupId, IFormFile avatarFile)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<string>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }

        if (avatarFile == null || avatarFile.Length == 0)
        {
            return ApiResponse<string>.Fail("FILE_EMPTY", "Vui lòng chọn một file ảnh.");
        }

        if (!avatarFile.ContentType.ToLowerInvariant().StartsWith("image/"))
        {
            return ApiResponse<string>.Fail("INVALID_CONTENT_TYPE", "File được tải lên không phải là một định dạng ảnh hợp lệ.");
        }

        var maxFileSizeMb = _settingsService.Get<int>(SettingKeys.MaxAvatarSizeMb, 3);
        var maxFileSizeBytes = maxFileSizeMb * 1024 * 1024;

        var allowedTypesCsv = _settingsService.Get<string>(SettingKeys.AllowedFileTypes, "jpg,png,jpeg,webp");
        var allowedTypeList = allowedTypesCsv.Split(',').Select(t => t.Trim().ToLower()).ToList();
        if (avatarFile.Length > maxFileSizeBytes)
        {
            return ApiResponse<string>.Fail("FILE_TOO_LARGE", $"Kích thước file không được vượt quá {maxFileSizeMb}MB.");
        }
        var extension = Path.GetExtension(avatarFile.FileName).TrimStart('.').ToLowerInvariant();
        if (!allowedTypeList.Contains(extension))
        {
            return ApiResponse<string>.Fail("INVALID_FILE_TYPE", $"Chỉ chấp nhận các định dạng file: {allowedTypesCsv}.");
        }

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
        {
            return ApiResponse<string>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");
        }

        var oldAvatarUrl = group.GroupAvatarUrl;

        var storageService = _storageStrategy.GetStorageService(avatarFile.ContentType);
        var newAvatarUrl = await storageService.UploadAsync(avatarFile, "group-avatars");

        group.GroupAvatarUrl = newAvatarUrl.Url;
        _unitOfWork.Groups.Update(group);
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldAvatarUrl))
        {
            // var oldFileStorageService = _storageStrategy.GetStorageServiceForUrl(oldAvatarUrl);
            // await oldFileStorageService.DeleteAsync(oldAvatarUrl);
        }

        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId, _currentUser.FullName,
            EnumAdminActionType.GroupAvatarUpdated,
            EnumTargetEntityType.Group, groupId.ToString(),
            $"Admin đã cập nhật ảnh đại diện cho nhóm '{group.GroupName}'.",
            null
        ));

        return ApiResponse<string>.Ok(newAvatarUrl.Url, "Cập nhật ảnh đại diện thành công.");
    }

    public async Task<ApiResponse<object>> RestorePostAsync(int postId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");

        // Dùng IgnoreQueryFilters để tìm được bài viết đã bị xóa
        var post = await _unitOfWork.Posts.GetQueryable()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.PostID == postId);

        if (post == null)
            return ApiResponse<object>.Fail("POST_NOT_FOUND", "Không tìm thấy bài đăng.");

        if (!post.IsDeleted)
            return ApiResponse<object>.Fail("POST_NOT_DELETED", "Bài viết này không ở trong trạng thái bị xóa.");

        post.IsDeleted = false;
        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
            adminId, _currentUser.FullName,
            EnumAdminActionType.PostRestored, // Enum mới cho hành động này
            EnumTargetEntityType.Post, postId.ToString(),
            $"Admin đã khôi phục bài viết có tiêu đề: '{post.Title}'.",
            null
        ));

        return ApiResponse<object>.Ok(null, "Khôi phục bài đăng thành công.");
    }

    public ApiResponse<object> PerformBulkGroupActionAsync(BulkGroupActionRequest request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var adminId) || string.IsNullOrEmpty(_currentUser.FullName))
            return ApiResponse<object>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");

        var adminFullName = _currentUser.FullName;
        var batchId = Guid.NewGuid();

        // Lặp qua danh sách GroupIds và xếp hàng các công việc vào background job
        foreach (var groupId in request.GroupIds.Distinct())
        {
            switch (request.Action)
            {
                case EnumBulkGroupActionType.Archive:
                    _backgroundJobClient.Enqueue(() => ArchiveGroupAsAdminAsync(groupId, adminId, adminFullName, batchId));
                    break;
                case EnumBulkGroupActionType.Unarchive:
                    _backgroundJobClient.Enqueue(() => UnarchiveGroupAsAdminAsync(groupId, adminId, adminFullName, batchId));
                    break;
                case EnumBulkGroupActionType.SoftDelete:
                    _backgroundJobClient.Enqueue(() => SoftDeleteGroupAsync(groupId, adminId, adminFullName, batchId)); // Giả sử bạn có hàm này
                    break;
                case EnumBulkGroupActionType.Restore:
                    _backgroundJobClient.Enqueue(() => RestoreGroupAsAdminAsync(groupId, adminId, adminFullName, batchId));
                    break;
            }

            var finalJobId = _backgroundJobClient.Schedule<IAdminNotificationService>(
            service => service.SendBulkActionCompletionNotificationAsync(
                adminId,
                batchId,
                request.GroupIds.Count,
                request.Action.ToString()
            ),
            TimeSpan.FromSeconds(10) // Delay một khoảng thời gian hợp lý để các job kia chạy
    );
        }

        return ApiResponse<object>.Ok(null, $"Yêu cầu xử lý hàng loạt cho {request.GroupIds.Count} nhóm đã được tiếp nhận.");
    }

    public async Task<ApiResponse<GroupDto>> CreateGroupAsAdminAsync(CreateGroupAsAdminRequest request)
    {
        if (!Guid.TryParse(_currentUser.Id, out var systemAdminId) || string.IsNullOrEmpty(_currentUser.FullName))
        {
            return ApiResponse<GroupDto>.Fail("ADMIN_CONTEXT_INVALID", "Không xác định được thông tin Admin.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var newGroup = new Group
            {
                GroupID = Guid.NewGuid(),
                GroupName = request.GroupName,
                Description = request.Description,
                GroupType = request.GroupType, 
                CreatedByUserID = systemAdminId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Groups.AddAsync(newGroup);

            foreach (var userId in request.InitialAdminUserIds)
            {
                var userExists = await _userManager.FindByIdAsync(userId.ToString()) != null;
                if (!userExists)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ApiResponse<GroupDto>.Fail("USER_NOT_FOUND", $"Người dùng với ID {userId} không tồn tại.");
                }

                var newMember = new GroupMember
                {
                    GroupID = newGroup.GroupID,
                    UserID = userId,
                    Role = EnumGroupRole.Admin,
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.GroupMembers.AddAsync(newMember);
            }

            var newConversation = new Conversation
            {
                ConversationType = EnumConversationType.Group,
                ExplicitGroupID = newGroup.GroupID,
                Title = newGroup.GroupName,
                AvatarUrl = newGroup.GroupAvatarUrl
            };
            await _unitOfWork.Conversations.AddAsync(newConversation);
            foreach (var adminUserId in request.InitialAdminUserIds)
            {
                var newParticipant = new ConversationParticipants
                {                  
                    Conversation = newConversation,
                    UserID = adminUserId,
                    JoinedAt = DateTime.UtcNow
                };
                await _unitOfWork.ConversationParticipants.AddAsync(newParticipant);
            }

            // 6. Lưu tất cả thay đổi
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            var groupDto = new GroupDto
            {
                GroupId = newGroup.GroupID,
                GroupName = newGroup.GroupName,
                Description = newGroup.Description,
                GroupType = newGroup.GroupType,
                GroupAvatarUrl = newGroup.GroupAvatarUrl,
                CreatedByUserID = newGroup.CreatedByUserID,
                CreatedAt = newGroup.CreatedAt
            };

            // 7. Ghi Log Kiểm toán
            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                systemAdminId, _currentUser.FullName,
                EnumAdminActionType.GroupCreated,
                EnumTargetEntityType.Group, newGroup.GroupID.ToString(),
                $"Admin tổng đã tạo nhóm '{newGroup.GroupName}'.",
                null
            ));

            return ApiResponse<GroupDto>.Ok(groupDto, "Tạo nhóm thành công.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Lỗi khi Admin tổng tạo nhóm.");
            return ApiResponse<GroupDto>.Fail("CREATE_GROUP_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }

    public async Task<ApiResponse<PagedResult<GroupMemberSearchResultDto>>> SearchMembersInGroupAsync(Guid groupId, SearchGroupMembersParams request)
    {
        var groupExists = await _unitOfWork.Groups.GetQueryable().AnyAsync(g => g.GroupID == groupId);
        if (!groupExists)
        {
            return ApiResponse<PagedResult<GroupMemberSearchResultDto>>.Fail("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");
        }

        var query = _unitOfWork.GroupMembers.GetQueryable()
            .AsNoTracking()
            .Where(gm => gm.GroupID == groupId); // << Lọc theo nhóm cụ thể

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = $"%{request.SearchTerm.Trim()}%";
            // Cần join với bảng User để tìm kiếm theo tên/email
            query = query.Where(gm =>
                (gm.User.FullName != null && EF.Functions.Like(gm.User.FullName, term)) ||
                (gm.User.Email != null && EF.Functions.Like(gm.User.Email, term))
            );
        }

        var pagedResult = await query
            .OrderBy(gm => gm.User.FullName)
            .Select(gm => new GroupMemberSearchResultDto
            {
                UserId = gm.UserID,
                FullName = gm.User.FullName,
                AvatarUrl = gm.User.AvatarUrl,
                RoleInGroup = gm.Role
            })
            .ToPagedResultAsync(request.PageNumber, request.PageSize);

        return ApiResponse<PagedResult<GroupMemberSearchResultDto>>.Ok(pagedResult);
    }

    public async Task<ApiResponse<PostAdminDetailDto>> GetPostDetailsAsAdminAsync(int postId)
    {
        try
        {
            var post = await _unitOfWork.Posts.GetQueryable()
                .IgnoreQueryFilters()
                .Where(p => p.PostID == postId)
                .Include(p => p.Author)
                .Include(p => p.Group)
                .FirstOrDefaultAsync();

            if (post == null)
            {
                return ApiResponse<PostAdminDetailDto>.Fail("POST_NOT_FOUND", "Không tìm thấy bài viết.");
            }


            var commentsTask = _unitOfWork.PostComments.GetQueryable()
                .IgnoreQueryFilters().Where(c => c.PostID == postId).Include(c => c.User)
                .OrderBy(c => c.CreatedAt).Select(c => new PostCommentAdminDto {
                    CommentId = c.CommentID,
                    AuthorId = c.UserID,
                    AuthorFullName = c.User!.FullName!,
                    AuthorAvatarUrl = c.User.AvatarUrl,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    IsDeleted = c.IsDeleted,
                }).ToListAsync();

            var reportsTask = _unitOfWork.ContentReports.GetQueryable()
                .Where(r => r.ReportedContentID == postId && r.ReportedContentType == EnumReportedContentType.Post)
                .Include(r => r.ReportedByUser).OrderByDescending(r => r.CreatedAt)
                .Select(r => new PostReportDto {
                    ReportId = r.ContentReportID,
                    ReportedByUserId = r.ReportedByUserID,
                    ReportedByUserFullName = r.ReportedByUser!.FullName!,
                    Reason = r.Reason,
                    Status = r.Status,
                    ReportedAt = r.CreatedAt
                }).ToListAsync();

            var likeCountTask = _unitOfWork.PostLikes.GetQueryable().CountAsync(l => l.PostID == postId);

            await Task.WhenAll(commentsTask, reportsTask, likeCountTask);

            var comments = await commentsTask;
            var reports = await reportsTask;
            var likeCount = await likeCountTask;

            var postDetailDto = new PostAdminDetailDto
            {
                PostId = post.PostID,
                Title = post.Title,
                Content = post.ContentJson, // Trả về nội dung gốc
                SanitizedContentHtml = _htmlSanitizer.Sanitize(post.ContentJson), // Trả về nội dung đã làm sạch
                IsPinned = post.IsPinned,
                IsDeleted = post.IsDeleted,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                AuthorId = post.AuthorUserID,
                AuthorFullName = post.Author!.FullName!,
                AuthorAvatarUrl = post.Author.AvatarUrl,
                GroupId = post.GroupID,
                GroupName = post.Group!.GroupName,
                LikeCount = likeCount,
                CommentCount = comments.Count,
                Comments = comments,
                Reports = reports
            };

            return ApiResponse<PostAdminDetailDto>.Ok(postDetailDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy chi tiết bài viết cho Admin, PostId: {PostId}", postId);
            return ApiResponse<PostAdminDetailDto>.Fail("GET_POST_DETAIL_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }

    public async Task<ApiResponse<object>> CreateChatGroupAsAdminAsync(CreateChatGroupAsAdminDto dto)
    {
        var systemAdminId = Guid.Parse(_currentUser.Id!);

        if (dto.GroupType == EnumGroupType.Community)
            return ApiResponse<object>.Fail("Validation", "Loại nhóm không hợp lệ cho nhóm chat.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Lấy các user sẽ làm admin
            var adminUsers = await _unitOfWork.Users.GetQueryable()
                .Where(u => dto.InitialAdminUserIds.Contains(u.Id)).ToListAsync();

            // Tạo Group và Conversation
            var newGroup = new Group
            {
                GroupName = dto.GroupName,
                Description = dto.Description,
                GroupType = dto.GroupType,
                Privacy = dto.GroupType == EnumGroupType.Public ? EnumGroupPrivacy.Public : EnumGroupPrivacy.Private,
                CreatedByUserID = systemAdminId
            };

            var conversation = new Conversation
            {
                ConversationType = EnumConversationType.Group,
                Title = newGroup.GroupName,
                Group = newGroup
            };

            // Thêm các admin vào cả Members và Participants
            newGroup.Members = adminUsers.Select(u => new GroupMember { User = u, Role = EnumGroupRole.Admin }).ToList();
            conversation.Participants = adminUsers.Select(u => new ConversationParticipants { User = u }).ToList();

            await _unitOfWork.Groups.AddAsync(newGroup);
            await _unitOfWork.Conversations.AddAsync(conversation);

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();


            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                systemAdminId, _currentUser.FullName,
                EnumAdminActionType.GroupCreated,
                EnumTargetEntityType.Group, newGroup.GroupID.ToString(),
                $"Admin tổng đã tạo nhóm '{newGroup.GroupName}'.",
                null
            ));
            return ApiResponse<object>.Ok(_mapper.Map<GroupDto>(newGroup), "Tạo nhóm chat thành công.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogInformation(ex, "Lỗi khi Admin tổng tạo nhóm chat.");
            return ApiResponse<object>.Fail("SERVER_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }

    public async Task<ApiResponse<object>> CreateCommunityGroupAsAdminAsync(CreateCommunityGroupAsAdminDto dto)
    {
        var systemAdminId = Guid.Parse(_currentUser.Id!);

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var adminUsers = await _unitOfWork.Users.GetQueryable()
                .Where(u => dto.InitialAdminUserIds.Contains(u.Id)).ToListAsync();

            var newGroup = new Group
            {
                GroupName = dto.GroupName,
                Description = dto.Description,
                GroupType = EnumGroupType.Community,
                Privacy = dto.Privacy,
                CreatedByUserID = systemAdminId
            };

            newGroup.Members = adminUsers.Select(u => new GroupMember { User = u, Role = EnumGroupRole.Admin }).ToList();

            await _unitOfWork.Groups.AddAsync(newGroup);
            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _backgroundJobClient.Enqueue<IAdminAuditLogService>(service => service.LogAdminActionAsync(
                systemAdminId, _currentUser.FullName,
                EnumAdminActionType.GroupCreated,
                EnumTargetEntityType.Group, newGroup.GroupID.ToString(),
                $"Admin tổng đã tạo nhóm '{newGroup.GroupName}'.",
                null
            ));
            return ApiResponse<object>.Ok(null, "Tạo nhóm cộng đồng thành công.");
        }
        catch (Exception ex) 
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogInformation(ex, "Lỗi khi Admin tổng tạo nhóm cộng đồng.");
            return ApiResponse<object>.Fail("SERVER_ERROR", "Đã có lỗi hệ thống xảy ra.");
        }
    }
}
