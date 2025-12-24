using FastBiteGroupMCA.Application.DTOs.Conversation;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.Invitation;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Http;


namespace FastBiteGroupMCA.Application.IServices;

public interface IGroupService
{
    Task<ApiResponse<GroupDetailsDTO>> GetGroupDetailsByIdAsyncPreView(Guid groupId);
    Task<ApiResponse<object>> ArchiveGroupAsync(Guid groupId);
    Task<ApiResponse<object>> UnarchiveGroupAsync(Guid groupId);
    Task<ApiResponse<object>> DeleteGroupAsync(Guid groupId);
    Task<ApiResponse<object>> KickMemberAsync(Guid groupId, Guid userIdToKick);
    Task<ApiResponse<object>> UpdateGroupInfoAsync(Guid groupId, UpdateGroupInfoDto dto);
    Task<ApiResponse<CreateGroupsResponseDTO>> CreateChatGroupAsync(CreateChatGroupDto dto);
    Task<ApiResponse<CreateGroupsResponseDTO>> CreateCommunityGroupAsync(CreateCommunityGroupDto dto);
    Task<ApiResponse<object>> AddMemberAsync(Guid groupId, Guid userIdToAdd);
    // US-CUS-24: Danh sách thành viên với PresenceStatus
    Task<ApiResponse<PagedResult<GroupMemberListDto>>> GetGroupMembersAsync(Guid groupId, GetGroupMembersQuery query);
    Task<ApiResponse<PagedResult<UserGroupDTO>>> GetUserAssociatedGroupsAsync(GetUserGroupsQuery query);
    // mới
    Task<ApiResponse<PagedResult<PublicGroupDto>>> GetPublicGroupsAsync(GetPublicGroupsQuery query);
    Task<ApiResponse<JoinGroupPublicResponseDTO>> JoinPublicGroupAsync(Guid groupId);
    Task<ApiResponse<object>> SendInvitationsAsync(Guid groupId, SendInvitationsDto dto);
    Task<ApiResponse<InviteLinkDTO>> CreateInviteLinkAsync(Guid groupId, CreateInviteLinkDTO dto);
    Task<ApiResponse<GroupDetailsDTO>> GetGroupDetailsByIdAsync(Guid groupId);
    Task<ApiResponse<UpdateRoleResponseDTO>> ManageMemberRoleAsync(Guid groupId, Guid memberId, ManageMemberDTO dto);
    Task<ApiResponse<object>> LeaveGroupAsync(Guid groupId);
    /// <summary>
    /// Xử lý việc Admin cuối cùng chuyển quyền và rời nhóm.
    /// </summary>
    Task<ApiResponse<object>> TransferAndLeaveAsync(Guid groupId, TransferAndLeaveDTO dto);
    Task<ApiResponse<List<MentionSuggestionDTO>>> GetMentionSuggestionsAsync(Guid groupId, string? searchTerm);
    /// <summary>
    /// Cập nhật ảnh đại diện cho một nhóm.
    /// </summary>
    Task<ApiResponse<UpdateGroupAvatarResponseDTO>> UpdateGroupAvatarAsync(Guid groupId, IFormFile avatarFile);
}
