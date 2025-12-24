using FastBiteGroupMCA.Application.DTOs.Auth;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.IServices.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace FastBiteGroupMCA.Infastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IAdminNotificationService _adminNotificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISettingsService _settingsService;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IEmailService emailService,
        IOtpService otpService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AuthService> logger,
        IConfiguration configuration,
        IAdminNotificationService adminNotificationService,
        ISettingsService settingsService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _otpService = otpService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        _adminNotificationService = adminNotificationService;
        _settingsService = settingsService;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return ApiResponse<string>.Fail("INVALID_USER", "Người dùng không hợp lệ.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<string>.Fail(ErrorCodes.NotFound, "Không tìm thấy người dùng.");
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                return ApiResponse<string>.Fail("INVALID_TOKEN", "Token xác nhận không hợp lệ hoặc đã hết hạn.");
            }

            await _emailService.SendWelcomeEmailAsync(user.Email!, user.FisrtName);

            await _adminNotificationService.CreateAndBroadcastNotificationAsync(
                EnumAdminNotificationType.NewUserRegistered,
                $"Một người dùng mới, '{user.FullName}', vừa đăng ký.",
                $"/admin/users/{user.Id}/details",
                user.Id
            );

            _logger.LogInformation("Email confirmed for user {UserId}", userId);
            return ApiResponse<string>.Ok("Xác nhận email thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
            return ApiResponse<string>.Fail("CONFIRMATION_ERROR", "Đã có lỗi xảy ra trong quá trình xác nhận email.");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.Fail("INVALID_CREDENTIALS", "Tài khoản đã bị khóa vui lòng liên hệ với Admin");
            }
            if(user.IsDeleted)
            {
                return ApiResponse<AuthResponseDto>.Fail("INVALID_CREDENTIALS", "Tài khoản không tồn tại");
            }


            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return ApiResponse<AuthResponseDto>.Fail("EMAIL_NOT_CONFIRMED", "Vui lòng xác nhận email trước khi đăng nhập.");
            }



            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return ApiResponse<AuthResponseDto>.Fail("ACCOUNT_LOCKED", "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần.");
                }
                return ApiResponse<AuthResponseDto>.Fail("INVALID_CREDENTIALS", "Email hoặc mật khẩu không chính xác.");
            }

            // Check if 2FA is enabled
            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                await SendTwoFactorCodeAsync(request.Email);

                var response = new AuthResponseDto
                {
                    RequiresTwoFactor = true,
                    User = _mapper.Map<UserDto>(user)
                };

                return ApiResponse<AuthResponseDto>.Ok(response, "Vui lòng nhập mã xác thực 2FA.");
            }
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

            var loginHistory = new LoginHistory
            {
                UserId = user.Id,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                WasSuccessful = result.Succeeded,
                LoginTimestamp = DateTime.UtcNow
            };
            await _unitOfWork.LoginHistories.AddAsync(loginHistory);
            await _unitOfWork.SaveChangesAsync();
            // Generate tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, request.RememberMe);

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);

            var authResponse = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddHours(7),
                RequiresTwoFactor = false,
                User = userDto
            };

            _logger.LogInformation("User {Email} logged in successfully", request.Email);
            return ApiResponse<AuthResponseDto>.Ok(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return ApiResponse< AuthResponseDto >.Fail("LOGIN_ERROR", "Đã có lỗi xảy ra trong quá trình đăng nhập.");
        }
    }

    public async Task<ApiResponse<object>> LogoutAsync(LogoutDTO dto)
    {
        try
        {
            await _tokenService.RevokeRefreshTokenAsync(dto.RefreshToken);

            _logger.LogInformation("User logged out successfully by revoking refresh token.");
            return ApiResponse<object>.Ok(null!, "Đăng xuất thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout.");
            return ApiResponse<object>.Ok(null!, "Đăng xuất thành công. nhưng có lỗi xảy ra");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto request)
    {
        try
        {
            var refreshToken = await _unitOfWork.RefreshToken.GetValidTokenAsync(request.Token);
            if (refreshToken == null)
            {
                return ApiResponse<AuthResponseDto>.Fail("INVALID_TOKEN", "Refresh token không hợp lệ.");
            }

            var user = refreshToken.User;
            if (user == null || !user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.Fail("USER_INACTIVE", "Tài khoản không hoạt động.");
            }

            // Mark current token as used
            refreshToken.IsUsed = true;
            _unitOfWork.RefreshToken.Update(refreshToken);

            // Generate new tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            await _unitOfWork.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);

            var authResponse = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddHours(7),
                RequiresTwoFactor = false,
                User = userDto
            };

            _logger.LogInformation("Token refreshed for user {UserId}", user.Id);
            return ApiResponse<AuthResponseDto>.Ok(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return ApiResponse<AuthResponseDto>.Fail("REFRESH_ERROR", "Đã có lỗi xảy ra trong quá trình làm mới token.");
        }
    }

    public async Task<ApiResponse<string>> RegisterAsync(RegisterDto dto)
    {
        try
        {
            var allowRegistrations = _settingsService.Get<bool>(SettingKeys.AllowNewRegistrations, true);
            if (!allowRegistrations)
            {
                return ApiResponse<string>.Fail("REGISTRATION_DISABLED", "Tính năng đăng ký mới đã tạm thời bị tắt.");
            }

            if (await _unitOfWork.Users.EmailExistsAsync(dto.Email))
            {
                return ApiResponse<string>.Fail("USER_EXISTS", "Email đã được sử dụng.");
            }

            var user = _mapper.Map<AppUser>(dto);
            user.MessagingPrivacy = EnumMessagingPrivacy.FromAnyone;
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
                return ApiResponse<string>.Fail(errors);
            }

            // Assign default role
            var defaultRole = _settingsService.Get<string>(SettingKeys.DefaultRoleForNewUsers, "Customer");
            var result2 = await _userManager.AddToRoleAsync(user, defaultRole);
            if (!result2.Succeeded)
            {
                var errors = result2.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
                return ApiResponse<string>.Fail(errors);
            }

            // Send confirmation email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var baseUrl = _configuration["AppUrls:FrontendBaseUrl"];
            var feConfirmationLink = $"{baseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

            await _emailService.SendEmailConfirmationAsync(user.Email!, feConfirmationLink);

            _logger.LogInformation("User {Email} registered successfully", dto.Email);
            return ApiResponse<string>.Ok("Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {Email}", dto.Email);
            return ApiResponse<string>.Fail("REGISTRATION_ERROR", "Đã có lỗi xảy ra trong quá trình đăng ký.");
        }
    }

    public async Task<ApiResponse<string>> ResendConfirmationEmailAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return ApiResponse<string>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return ApiResponse<string>.Fail("EMAIL_ALREADY_CONFIRMED", "Email đã được xác nhận.");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            // Tạo link xác nhận email
            var baseUrl = _configuration["AppUrls:FrontendBaseUrl"];  // Sẽ tự động lấy
            var confirmationLink = $"{baseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

            await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationLink);

            _logger.LogInformation("Confirmation email resent to {Email}", email);
            return ApiResponse<string>.Ok("Email xác nhận đã được gửi lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending confirmation email to {Email}", email);
            return ApiResponse<string>.Fail("RESEND_ERROR", "Đã có lỗi xảy ra khi gửi lại email xác nhận.");
        }
    }

    public async Task<ApiResponse<string>> RevokeTokenAsync(string token)
    {
        try
        {
            await _tokenService.RevokeRefreshTokenAsync(token);

            _logger.LogInformation("Token revoked successfully");
            return ApiResponse<string>.Ok("Token đã được thu hồi thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return ApiResponse<string>.Fail("REVOKE_ERROR", "Đã có lỗi xảy ra trong quá trình thu hồi token.");
        }
    }

    public async Task<ApiResponse<string>> SendTwoFactorCodeAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return ApiResponse<string>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            }

            var otp = await _otpService.GenerateOtpAsync($"2fa:{email}");
            await _emailService.SendTwoFactorCodeAsync(email, otp);

            _logger.LogInformation("2FA code sent to {Email}", email);
            return ApiResponse<string>.Ok("Mã xác thực đã được gửi về email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending 2FA code to {Email}", email);
            return ApiResponse<string>.Fail("SEND_CODE_ERROR", "Đã có lỗi xảy ra khi gửi mã xác thực.");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> VerifyTwoFactorAsync(VerifyOtpDto request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Fail("USER_NOT_FOUND", "Không tìm thấy người dùng.");
            }

            var isValidOtp = await _otpService.ValidateOtpAsync($"2fa:{user.Email}", request.Code);
            if (!isValidOtp)
            {
                return ApiResponse<AuthResponseDto>.Fail("INVALID_OTP", "Mã OTP không chính xác hoặc đã hết hạn.");
            }

            // Generate tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, request.RememberMe);

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);

            var authResponse = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddHours(7),
                RequiresTwoFactor = false,
                User = userDto
            };

            _logger.LogInformation("2FA verification successful for user {Email}", request.Email);
            return ApiResponse<AuthResponseDto>.Ok(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA verification for {Email}", request.Email);
            return ApiResponse<AuthResponseDto>.Fail("VERIFICATION_ERROR", "Đã có lỗi xảy ra trong quá trình xác thực.");
        }
    }
    public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        // Luôn trả về thành công kể cả khi không tìm thấy user để tránh lộ thông tin email nào đã đăng ký.
        if (user != null)
        {
            var otp = await _otpService.GenerateOtpAsync($"reset-password:{dto.Email}");
            await _emailService.SendPasswordResetOtpAsync(dto.Email, otp);
        }
        return ApiResponse<object>.Ok(null, "Nếu email của bạn tồn tại trong hệ thống, chúng tôi đã gửi một mã khôi phục.");
    }

    public async Task<ApiResponse<VerifyResetOtpResponseDTO>> VerifyResetOtpAsync(VerifyResetPassOtpDTO dto)
    {
        var isValid = await _otpService.ValidateOtpAsync($"reset-password:{dto.Email}", dto.Otp);
        if (!isValid)
        {
            return ApiResponse<VerifyResetOtpResponseDTO>.Fail("InvalidOtp", "Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            // Trường hợp hiếm gặp nhưng vẫn cần xử lý
            return ApiResponse<VerifyResetOtpResponseDTO>.Fail("UserNotFound", "Không tìm thấy người dùng.");
        }

        // Tạo một token reset của ASP.NET Core Identity
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));

        return ApiResponse<VerifyResetOtpResponseDTO>.Ok(new VerifyResetOtpResponseDTO { ResetToken = encodedToken });
    }

    public async Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return ApiResponse<object>.Fail("InvalidRequest", "Yêu cầu không hợp lệ.");
        }

        try
        {
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.ResetToken));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new ApiError(e.Code, e.Description)).ToList();
                return ApiResponse<object>.Fail(errors);
            }

            return ApiResponse<object>.Ok(null, "Đặt lại mật khẩu thành công.");
        }
        catch (Exception)
        {
            // Xảy ra khi token bị sai định dạng Base64Url
            return ApiResponse<object>.Fail("InvalidToken", "Token khôi phục không hợp lệ.");
        }
    }
}
