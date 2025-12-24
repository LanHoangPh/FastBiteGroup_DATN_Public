using FastBiteGroupMCA.Application.DTOs.Auth;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    // === Nhóm 1: Xác thực & Đăng ký ===

    /// <summary>
    /// Đăng ký một tài khoản người dùng mới.
    /// </summary>
    /// <remarks>
    /// Sau khi đăng ký thành công, hệ thống sẽ tự động gửi một email xác nhận đến địa chỉ email đã cung cấp.
    /// Người dùng cần nhấp vào link trong email để kích hoạt tài khoản.
    /// </remarks>
    /// <response code="200">Đăng ký thành công, yêu cầu người dùng kiểm tra email để xác nhận.</response>
    /// <response code="400">Dữ liệu không hợp lệ (ví dụ: email đã tồn tại, mật khẩu yếu).</response>
    [HttpPost("register")]
    [Tags("1. Xác thực & Đăng ký")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Xác nhận email sau khi người dùng đăng ký.
    /// </summary>
    /// <remarks>
    /// Endpoint này được gọi khi người dùng nhấp vào link trong email xác nhận.
    /// Sau khi thành công, bạn nên điều hướng người dùng đến trang đăng nhập.
    /// </remarks>
    /// <param name="userId">ID của người dùng từ query string.</param>
    /// <param name="token">Token xác nhận từ query string.</param>
    /// <response code="200">Xác nhận email thành công.</response>
    /// <response code="400">UserId hoặc token không hợp lệ.</response>
    [HttpGet("confirm-email")]
    [Tags("1. Xác thực & Đăng ký")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var result = await _authService.ConfirmEmailAsync(userId, token);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Đăng nhập vào hệ thống.
    /// </summary>
    /// <remarks>
    /// Khi đăng nhập thành công, API sẽ trả về một Access Token (JWT) và một Refresh Token.
    /// Nếu tài khoản yêu cầu 2FA, API sẽ trả về isTwoFactorRequired = true và không có token.
    /// </remarks>
    /// <response code="200">Đăng nhập thành công và trả về tokens, hoặc yêu cầu 2FA.</response>
    /// <response code="400">Sai email hoặc mật khẩu, hoặc tài khoản chưa được kích hoạt.</response>
    [HttpPost("login")]
    [Tags("1. Xác thực & Đăng ký")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var result = await _authService.LoginAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    

    /// <summary>
    /// Lấy một Access Token mới bằng Refresh Token.
    /// </summary>
    /// <remarks>Sử dụng khi Access Token cũ đã hết hạn.</remarks>
    /// <response code="200">Làm mới token thành công, trả về cặp token mới.</response>
    /// <response code="400">Refresh Token không hợp lệ hoặc đã bị thu hồi.</response>
    [HttpPost("refresh-token")]
    [Tags("2. Quản lý Tokens")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Đăng xuất và thu hồi Refresh Token.
    /// </summary>
    /// <remarks>API này sẽ vô hiệu hóa Refresh Token hiện tại để không thể dùng để làm mới Access Token được nữa.</remarks>
    /// <response code="204">Đăng xuất và thu hồi token thành công.</response>
    [HttpPost("logout")]
    [Authorize]
    [Tags("2. Quản lý Tokens")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutDTO dto)
    {
        var response = await _authService.LogoutAsync(dto);
        return Ok(response);
    }
    /// <summary>
    /// Thu hồi một Refresh Token đang hoạt động.
    /// </summary>
    /// <remarks>
    /// API này được sử dụng để vô hiệu hóa một Refresh Token cụ thể, thường là một phần của quy trình đăng xuất an toàn.
    /// Sau khi bị thu hồi, Refresh Token này sẽ không thể được sử dụng để lấy Access Token mới được nữa.
    /// Người dùng phải đăng nhập để có thể gọi API này.
    /// </remarks>
    /// <param name="request">Đối tượng chứa Refresh Token cần thu hồi.</param>
    /// <response code="204">Thu hồi token thành công. Không có nội dung trả về.</response>
    /// <response code="400">Refresh Token không hợp lệ hoặc không tìm thấy.</response>
    /// <response code="401">Chưa xác thực (Access Token không hợp lệ).</response>
    [HttpPost("revoke-token")]
    [Authorize]
    [Tags("2. Quản lý Tokens")] // Gom vào nhóm đã định nghĩa trước đó
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenDto request)
    {
        var result = await _authService.RevokeTokenAsync(request.Token);
        return result.Success ? Ok(result) : BadRequest(result);
    }


    /// <summary>
    /// Gửi mã OTP (cho 2FA) đến email người dùng.
    /// </summary>
    /// <remarks>Gọi API này sau khi `POST /login` trả về `isTwoFactorRequired = true`.</remarks>
    /// <response code="200">Gửi mã OTP thành công.</response>
    /// <response code="404">Không tìm thấy người dùng với email cung cấp.</response>
    [HttpPost("send-2fa-code")]
    [Tags("3. Xác thực hai yếu tố (2FA)")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendTwoFactorCode([FromBody] string email)
    {
        var result = await _authService.SendTwoFactorCodeAsync(email);
        return result.Success ? Ok(result) : BadRequest(result);
    }
    /// <summary>
    /// Xác thực mã OTP để hoàn tất đăng nhập 2FA.
    /// </summary>
    /// <response code="200">Xác thực OTP thành công, trả về cặp token mới.</response>
    /// <response code="400">Mã OTP không chính xác hoặc đã hết hạn.</response>
    [HttpPost("verify-2fa")]
    [Tags("3. Xác thực hai yếu tố (2FA)")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyOtpDto request)
    {
        var result = await _authService.VerifyTwoFactorAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }


    /// <summary>
    /// Bước 1: Yêu cầu đặt lại mật khẩu khi quên.
    /// </summary>
    /// <remarks>Hệ thống sẽ gửi một email chứa mã OTP đến người dùng. API luôn trả về 200 OK để tránh lộ thông tin email có tồn tại trong hệ thống hay không.</remarks>
    /// <response code="200">Yêu cầu đã được xử lý.</response>
    [HttpPost("forgot-password")]
    [Tags("4. Khôi phục Mật khẩu")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
    {
        var response = await _authService.ForgotPasswordAsync(dto);
        return Ok(response); // Luôn trả về 200 OK
    }
    /// <summary>
    /// Bước 2: Xác thực mã OTP để đặt lại mật khẩu.
    /// </summary>
    /// <response code="200">Xác thực OTP thành công, người dùng có thể tiến đến bước 3.</response>
    /// <response code="400">Mã OTP không chính xác hoặc đã hết hạn.</response>
    [HttpPost("verify-reset-otp")]
    [Tags("4. Khôi phục Mật khẩu")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyResetOtp(VerifyResetPassOtpDTO dto)
    {
        var response = await _authService.VerifyResetOtpAsync(dto);
        return response.Success ? Ok(response) : BadRequest(response);
    }
    /// <summary>
    /// Bước 3: Đặt lại mật khẩu mới sau khi đã xác thực OTP.
    /// </summary>
    /// <response code="200">Đặt lại mật khẩu thành công.</response>
    /// <response code="400">Dữ liệu không hợp lệ (ví dụ: token OTP không đúng, mật khẩu yếu).</response>
    [HttpPost("reset-password")]
    [Tags("4. Khôi phục Mật khẩu")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordDTO dto)
    {
        var response = await _authService.ResetPasswordAsync(dto);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Gửi lại email xác nhận tài khoản.
    /// </summary>
    /// <remarks>Dành cho trường hợp người dùng không nhận được email xác nhận ban đầu hoặc link đã hết hạn.</remarks>
    /// <response code="200">Yêu cầu đã được xử lý.</response>
    [HttpPost("resend-confirmation")]
    [Tags("5. Tiện ích Tài khoản")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] string email)
    {
        var result = await _authService.ResendConfirmationEmailAsync(email);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
