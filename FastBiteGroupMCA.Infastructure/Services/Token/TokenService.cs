using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using FastBiteGroupMCA.Application.IServices.Auth;

namespace FastBiteGroupMCA.Infastructure.Services.Token;
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<AppUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public TokenService(
        IConfiguration configuration,
        UserManager<AppUser> userManager,
        IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateAccessTokenAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("firstName", user.FisrtName),
            new("lastName", user.LastName),
            new("fullName", user.FullName ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(_configuration["Jwt:ExpiryHours"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, bool rememberMe = false)
    {
        var refreshToken = new RefreshToken
        {
            Token = GenerateRandomToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(rememberMe ? 30 : 7),
            IsUsed = false,
            IsRevoked = false
        };

        await _unitOfWork.RefreshToken.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await _unitOfWork.RefreshToken.GetValidTokenAsync(token);
        return refreshToken != null;
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _unitOfWork.RefreshToken.GetByTokenAsync(token);
        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            _unitOfWork.RefreshToken.Update(refreshToken);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    private static string GenerateRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
