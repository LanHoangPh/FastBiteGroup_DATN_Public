using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Entities;
using System.Security.Claims;

namespace FastBiteGroupMCA.Application.IServices.Auth
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(AppUser user);
        Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, bool rememberMe = false);
        Task<bool> ValidateRefreshTokenAsync(string token);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        Task RevokeRefreshTokenAsync(string token);
    }
}
