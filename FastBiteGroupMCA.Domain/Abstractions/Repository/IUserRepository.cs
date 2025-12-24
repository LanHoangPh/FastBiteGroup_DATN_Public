namespace FastBiteGroupMCA.Domain.Abstractions.Repository;

public interface IUserRepository : IGenericRepository<AppUser>
{
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser?> GetByIdWithRefreshTokensAsync(Guid userId);
    Task<IEnumerable<AppUser>> GetPagedUsersAsync(int pageNumber, int pageSize, string? searchTerm = null);
    Task<long> GetTotalUsersCountAsync(string? searchTerm = null);
    Task<bool> EmailExistsAsync(string email);
    Task<AppUser?> GetByIdWithRolesAsync(Guid userId);
}
