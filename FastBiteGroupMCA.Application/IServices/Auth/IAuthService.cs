using FastBiteGroupMCA.Application.DTOs.Auth;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices.Auth
{
    public interface IAuthService
    {
        Task<ApiResponse<string>> RegisterAsync(RegisterDto request);
        Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto request);
        Task<ApiResponse<AuthResponseDto>> VerifyTwoFactorAsync(VerifyOtpDto request);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto request);
        Task<ApiResponse<string>> RevokeTokenAsync(string token);
        Task<ApiResponse<string>> SendTwoFactorCodeAsync(string email);
        Task<ApiResponse<string>> ResendConfirmationEmailAsync(string email);
        Task<ApiResponse<object>> LogoutAsync(LogoutDTO dto);

        Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordDTO dto);
        Task<ApiResponse<VerifyResetOtpResponseDTO>> VerifyResetOtpAsync(VerifyResetPassOtpDTO dto);
        Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordDTO dto);
    }
}
