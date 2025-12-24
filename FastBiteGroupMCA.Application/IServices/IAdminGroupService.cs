using FastBiteGroupMCA.Application.DTOs.Admin.Group;
using FastBiteGroupMCA.Application.DTOs.Admin.Post;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Domain.Enum;
using Microsoft.AspNetCore.Http;
namespace FastBiteGroupMCA.Application.IServices;

public interface IAdminGroupService
{
    Task<ApiResponse<object>> CreateChatGroupAsAdminAsync(CreateChatGroupAsAdminDto dto);
    Task<ApiResponse<object>> CreateCommunityGroupAsAdminAsync(CreateCommunityGroupAsAdminDto dto);
    ApiResponse<object> PerformBulkGroupActionAsync(BulkGroupActionRequest request);
    Task<ApiResponse<object>> RestorePostAsync(int postId);
    Task<ApiResponse<PagedResult<GroupForList_AdminDto>>> GetGroupsForAdminAsync(GetGroupsAdminParams request);
    /// <summary>
    /// [Admin] Lưu trữ một nhóm.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    Task<ApiResponse<bool>> ArchiveGroupAsAdminAsync(Guid groupId, Guid adminId, string adminFullName, Guid? batchId = null);
    /// <summary>
    /// [Admin] Bỏ lưu trữ một nhóm.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    /// 
    Task<ApiResponse<bool>> UnarchiveGroupAsAdminAsync(Guid groupId, Guid adminId, string adminFullName, Guid? batchId = null);
    Task<ApiResponse<bool>> SoftDeleteGroupAsync(Guid groupId, Guid adminId, string adminFullName,Guid? batchId = null);
    Task<ApiResponse<string>> UpdateGroupAvatarAsAdminAsync(Guid groupId, IFormFile avatarFile);
    Task<ApiResponse<object>> RestoreGroupAsAdminAsync(Guid groupId, Guid adminId, string adminFullName, Guid? batchId = null);
    Task<ApiResponse<object>> UpdateGroupInfoAsync(Guid groupId, UpdateGroupRequestDTO request);
    Task<ApiResponse<AdminGroupDetailDTO>> GetGroupDetailsAsync(Guid groupId);
    Task<ApiResponse<PagedResult<GroupAdminMemberDTO>>> GetGroupMembersAsync(Guid groupId, GetGroupMembersParams request);
    Task<ApiResponse<object>> AddMemberAsSystemAdminAsync(Guid groupId, AddMemberAdminRequest request);
    Task<ApiResponse<object>> UpdateMemberRoleAsync(Guid groupId, Guid userId, EnumGroupRole newRole);
    Task<ApiResponse<object>> RemoveMemberAsync(Guid groupId, Guid userId);
    Task<ApiResponse<PagedResult<PostForListDTO>>> GetGroupPostsAsync(Guid groupId, GetGroupPostsParams request);
    Task<ApiResponse<object>> DeletePostAsync(int postId);
    Task<ApiResponse<object>> UpdateGroupSettingsAsync(Guid groupId, UpdateGroupSettingsDTO dto);
    Task<ApiResponse<object>> ChangeGroupOwnerAsync(Guid groupId, Guid newOwnerUserId);
    Task<ApiResponse<GroupDto>> CreateGroupAsAdminAsync(CreateGroupAsAdminRequest request);
    Task<ApiResponse<PagedResult<GroupMemberSearchResultDto>>> SearchMembersInGroupAsync(Guid groupId, SearchGroupMembersParams request);
    Task<ApiResponse<PostAdminDetailDto>> GetPostDetailsAsAdminAsync(int postId);
}
