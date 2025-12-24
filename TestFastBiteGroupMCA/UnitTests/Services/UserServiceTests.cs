using AutoMapper;
using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.IServices.FileStorage;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Infastructure.Services;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace TestFastBiteGroupMCA.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<RoleManager<AppRole>> _roleManagerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<CloudinaryStorageService> _cloudinaryStorageServiceMock;
    private readonly Mock<StorageStrategy> _storageStrategyMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly IUserService _userService;

    public UserServiceTests()
    {
        _userManagerMock = CreateMockUserManager();
        _roleManagerMock = CreateMockRoleManager();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _cloudinaryStorageServiceMock = new Mock<CloudinaryStorageService>();
        _storageStrategyMock = new Mock<StorageStrategy>();
        _currentUserMock = new Mock<ICurrentUser>();
        _configurationMock = new Mock<IConfiguration>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
    }

    private static Mock<UserManager<AppUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<RoleManager<AppRole>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<AppRole>>();
        return new Mock<RoleManager<AppRole>>(store.Object, null, null, null, null);
    }

    private void SetupHttpContextWithUser(string userId = "a1b2c3d4-e5f6-7890-1234-567890abcdef")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "mock"));

        var httpContext = new DefaultHttpContext { User = user };
        _httpContextAccessorMock.Setup(_ => _.HttpContext).Returns(httpContext);
    }
}
