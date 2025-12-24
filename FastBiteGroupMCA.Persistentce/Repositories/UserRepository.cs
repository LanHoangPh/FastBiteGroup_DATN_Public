using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories
{
    public class UserRepository : GenericRepository<AppUser>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<AppUser?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<AppUser?> GetByIdWithRefreshTokensAsync(Guid userId)
        {
            return await _dbSet
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        }

        public async Task<AppUser?> GetByIdWithRolesAsync(Guid userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        }

        public async Task<IEnumerable<AppUser>> GetPagedUsersAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            var query = _dbSet.Where(u => !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u =>
                    u.Email!.Contains(searchTerm) ||
                    u.FisrtName.Contains(searchTerm) ||
                    u.LastName.Contains(searchTerm) ||
                    (u.FullName != null && u.FullName.Contains(searchTerm)));
            }

            return await query
                .OrderBy(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<long> GetTotalUsersCountAsync(string? searchTerm = null)
        {
            var query = _dbSet.Where(u => !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u =>
                    u.Email!.Contains(searchTerm) ||
                    u.FisrtName.Contains(searchTerm) ||
                    u.LastName.Contains(searchTerm) ||
                    (u.FullName != null && u.FullName.Contains(searchTerm)));
            }

            return await query.CountAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email && !u.IsDeleted);
        }
    }
}
