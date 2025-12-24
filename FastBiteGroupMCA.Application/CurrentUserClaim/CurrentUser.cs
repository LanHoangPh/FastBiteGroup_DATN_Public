using FastBiteGroupMCA.Application.Helper;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FastBiteGroupMCA.Application.CurrentUserClaim;

public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _user;
    private readonly Lazy<IReadOnlyList<string>> _roles;


    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _user = httpContextAccessor.HttpContext?.User;

        _roles = new Lazy<IReadOnlyList<string>>(() =>
            _user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList().AsReadOnly() ?? new List<string>().AsReadOnly()
        );
    }

    public string? Id => _user?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => _user?.FindFirstValue(ClaimTypes.Name);

    public string? Email => _user?.FindFirstValue(ClaimTypes.Email);

    public string? FirstName => _user?.FindFirstValue(CustomClaimTypes.FirstName);

    public string? LastName => _user?.FindFirstValue(CustomClaimTypes.LastName);

    public string? FullName => _user?.FindFirstValue(CustomClaimTypes.FullName);

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles => _roles.Value;

    public bool IsInRole(string roleName)
    {
        return _user?.IsInRole(roleName) ?? false;
    }
}
