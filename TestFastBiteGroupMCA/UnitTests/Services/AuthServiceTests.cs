using FastBiteGroupMCA.Application.DTOs.Auth;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Enum;
using FastBiteGroupMCA.Infastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using FastBiteGroupMCA.Domain.Entities;

namespace TestFastBiteGroupMCA.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IAdminNotificationService> _adminNotificationServiceMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = CreateMockUserManager();
        _signInManagerMock = CreateMockSignInManager();
        _tokenServiceMock = new Mock<ITokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _otpServiceMock = new Mock<IOtpService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _configurationMock = new Mock<IConfiguration>();
        _adminNotificationServiceMock = new Mock<IAdminNotificationService>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _authService = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _otpServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _configurationMock.Object,
            _adminNotificationServiceMock.Object,
            _settingsServiceMock.Object,
            _httpContextAccessorMock.Object);
    }

    private static Mock<UserManager<AppUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<SignInManager<AppUser>> CreateMockSignInManager()
    {
        var userManagerMock = CreateMockUserManager();
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        return new Mock<SignInManager<AppUser>>(userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        var appUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            FisrtName = registerDto.FirstName,
            LastName = registerDto.LastName
        };

_settingsServiceMock.Setup(x => x.Get<bool>(It.IsAny<SettingKeys>(), It.IsAny<bool>())).Returns(true);
        _unitOfWorkMock.Setup(x => x.Users.EmailExistsAsync(registerDto.Email)).ReturnsAsync(false);
        _mapperMock.Setup(x => x.Map<AppUser>(registerDto)).Returns(appUser);
        _userManagerMock.Setup(x => x.CreateAsync(appUser, registerDto.Password)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(appUser, "Customer")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(appUser)).ReturnsAsync("test-token");
        _configurationMock.Setup(x => x["AppUrls:FrontendBaseUrl"]).Returns("http://localhost:3000");

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.", result.Data);
        _emailServiceMock.Verify(x => x.SendEmailConfirmationAsync(registerDto.Email, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        _settingsServiceMock.Setup(x => x.Get<bool>(It.IsAny<SettingKeys>(), It.IsAny<bool>())).Returns(true);
        _unitOfWorkMock.Setup(x => x.Users.EmailExistsAsync(registerDto.Email)).ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors!, e => e.ErrorCode == "USER_EXISTS");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!",
            RememberMe = false
        };

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email,
            EmailConfirmed = true,
            IsActive = true
        };

        var userDto = new UserDto { Id = user.Id, Email = user.Email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
        _settingsServiceMock.Setup(x => x.Get<bool>(It.IsAny<SettingKeys>(), It.IsAny<bool>())).Returns(true);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
        _tokenServiceMock.Setup(x => x.GenerateAccessTokenAsync(user)).ReturnsAsync("access-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(user.Id, loginDto.RememberMe))
            .ReturnsAsync(new RefreshToken { Token = "refresh-token" });
        _mapperMock.Setup(x => x.Map<UserDto>(user)).Returns(userDto);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Customer" });
        _unitOfWorkMock.Setup(x => x.LoginHistories.AddAsync(It.IsAny<LoginHistory>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("access-token", result.Data.AccessToken);
        Assert.Equal("refresh-token", result.Data.RefreshToken);
        Assert.False(result.Data.RequiresTwoFactor);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldReturnFailure()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword",
            RememberMe = false
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync((AppUser)null);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors!, e => e.ErrorCode == "INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = "dGVzdC10b2tlbg=="; // base64 encoded "test-token"
        var user = new AppUser
        {
            Id = Guid.Parse(userId),
            Email = "test@example.com",
            FisrtName = "John"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, "test-token")).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.ConfirmEmailAsync(userId, token);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Xác nhận email thành công.", result.Data);
        _emailServiceMock.Verify(x => x.SendWelcomeEmailAsync(user.Email!, user.FisrtName), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldAlwaysReturnSuccess()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDTO { Email = "test@example.com" };
        var user = new AppUser { Email = forgotPasswordDto.Email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(forgotPasswordDto.Email)).ReturnsAsync(user);
        _otpServiceMock.Setup(x => x.GenerateOtpAsync($"reset-password:{forgotPasswordDto.Email}")).ReturnsAsync("123456");

        // Act
        var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Nếu email của bạn tồn tại trong hệ thống", result.Message!);
        _emailServiceMock.Verify(x => x.SendPasswordResetOtpAsync(forgotPasswordDto.Email, "123456"), Times.Once);
    }
}
