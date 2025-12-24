using FastBiteGroupMCA.Application.DTOs.Admin;
using FastBiteGroupMCA.Application.DTOs.Admin.User;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices;

public interface IAdminUserService
{
    Task<ApiResponse<PagedResult<UserSearchResultDTO>>> SearchAvailableUsersAsync(UserSearchRequest request);
    /// <summary>
    /// [US-AD-11] Lấy thông tin chi tiết tổng quan của một người dùng cho Admin.
    /// </summary>
    Task<ApiResponse<AdminUserDetailDto>> GetUserDetailForAdminAsync(Guid userId);

    /// <summary>
    /// [US-AD-11] Lấy danh sách hoạt động (phân trang) của một người dùng cho Admin.
    /// </summary>
    Task<ApiResponse<PagedResult<UserActivityDTO>>> GetUserActivityForAdminAsync(Guid userId, GetUserActivityParams request);
    Task<ApiResponse<object>> DeactivateUserAccountAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, string reason, Guid? batchId = null);
    Task<ApiResponse<object>> ReactivateUserAccountAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, Guid? batchId = null);
    Task<ApiResponse<object>> ForcePasswordResetAsync(Guid userId);

    Task<ApiResponse<PagedResult<AdminUserListItemDTO>>> GetUsersForAdminAsync(GetUsersAdminParams request);
    Task<ApiResponse<MyAdminProfileDto>> GetMyAdminProfileAsync();
    Task<ApiResponse<object>> CreateUserAsAdminAsync(CreateUserByAdminRequest request);
    Task<ApiResponse<object>> UpdateUserBasicInfoAsync(Guid userId, UpdateUserBasicInfoRequest request);
    Task<ApiResponse<object>> RemoveUserAvatarAsync(Guid userId);
    Task<ApiResponse<object>> RemoveUserBioAsync(Guid userId);
    Task<ApiResponse<object>> DeleteUserAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, Guid? batchId = null);
    Task<ApiResponse<object>> RestoreUserAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, Guid? batchId = null);
    Task<ApiResponse<object>> AssignRoleAsync(Guid userId, byte[] rowVersion, Guid adminId, string adminFullName, string roleName, Guid? batchId = null);
    Task<ApiResponse<object>> RemoveRoleAsync(Guid userId, byte[] rowVersion, string roleName);
    ApiResponse<object> PerformBulkUserActionAsync(BulkUserActionRequest request);
}
