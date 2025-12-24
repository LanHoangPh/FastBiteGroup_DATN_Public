using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Http;

namespace FastBiteGroupMCA.Application.IServices.Auth;

public interface IUserService 
{
    Task<ApiResponse<UserDashboardStatsDto>> GetDashboardStatsAsync();
    /// <summary>
    /// Vô hiệu hóa tài khoản người dùng (chỉ dành cho quản trị viên bên cộng đồng).
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    Task<ApiResponse<object>> DeactivateUserAccountAsync(Guid userId, string reason);
    Task<ApiResponse<object>> UpdatePrivacySettingsAsync(UpdatePrivacySettingsDto dto);
    Task<ApiResponse<object>> DeactivateAccountAsync(DeactivateAccountDto dto);
    Task<ApiResponse<PagedResult<UserSearchResultDTO>>> SearchUsersForInviteAsync(UserSearchForInviteRequest request);
    /// <summary>
    /// Lấy 10 lần đăng nhập gần nhất của người dùng hiện tại.
    /// </summary>
    Task<ApiResponse<List<LoginHistoryDto>>> GetMyLoginHistoryAsync();
    Task<ApiResponse<MyProfileDto>> GetMyProfileAsync();
    Task<ApiResponse<object>> RequestAccountDeletionAsync(DeleteAccountRequestDto dto);
    Task<ApiResponse<UserDto>> UpdateProfileInfoAsync(UpdateUserADDto request);
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(PagedRequestDto request);
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid userId);
    Task<ApiResponse<string>> ChangeUserRoleAsync(Guid userId, Guid newRoleId);
    Task<ApiResponse<UpdateAvatarResponseDTO>> UpdateUserAvatarAsync(IFormFile avatarFile);

    Task<ApiResponse<object>> ChangePasswordAsync(ChangePasswordRequestDTO dto);
    Task<ApiResponse<object>> SubscribeToPushNotificationsAsync(Guid userId, string playerId);
    // ... các phương thức khác
    Task<ApiResponse<List<ContactDto>>> GetMyContactsAsync(Guid currentUserId);
    Task<ApiResponse<List<MutualGroupDto>>> GetMutualGroupsAsync(Guid partnerUserId);
}
