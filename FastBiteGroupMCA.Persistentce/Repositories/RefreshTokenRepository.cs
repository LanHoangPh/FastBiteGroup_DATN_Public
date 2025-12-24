using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<RefreshToken?> GetValidTokenAsync(string token)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.Token == token &&
                !rt.IsUsed &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<IEnumerable<RefreshToken>> GetUserActiveTokensAsync(Guid userId)
    {
        return await _dbSet
            .Where(rt =>
                rt.UserId == userId &&
                !rt.IsUsed &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task RevokeUserTokensAsync(Guid userId)
    {
        var tokens = await _dbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }
    }
    public async Task RevokeAllForUserAsync(Guid userId)
    {
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId &&
                         !rt.IsUsed && !rt.IsRevoked)
            .ExecuteUpdateAsync(setter => setter.SetProperty(rt => rt.IsRevoked, true));
    }
}
