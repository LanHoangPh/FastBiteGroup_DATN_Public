using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.Invitation;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices;

public interface IInvitationService
{
    Task<ApiResponse<object>> RevokeInvitationAsync(Guid groupId, int invitationId);
    Task<ApiResponse<PagedResult<SentGroupInvitationDTO>>> GetSentInvitationsByGroupAsync(Guid groupId, GetSentInvitationsQuery query);
    Task<ApiResponse<List<GroupInvitationDTO>>> GetPendingInvitationsAsync();
    Task<ApiResponse<object>> RespondToInvitationAsync(int invitationId, RespondToInvitationDTO dto);
    Task<ApiResponse<GroupPreviewDTO>> GetGroupPreviewByCodeAsync(string invitationCode);
    Task<ApiResponse<JoinGroupResponseDTO>> AcceptInviteLinkAsync(string invitationCode);
}
