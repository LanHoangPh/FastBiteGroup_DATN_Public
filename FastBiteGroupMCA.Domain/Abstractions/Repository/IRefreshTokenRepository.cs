namespace FastBiteGroupMCA.Domain.Abstractions.Repository
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<IEnumerable<RefreshToken>> GetUserActiveTokensAsync(Guid userId);
        Task RevokeUserTokensAsync(Guid userId);
        Task<RefreshToken?> GetValidTokenAsync(string token);
        /// <summary>
        /// Thu hồi tất cả các Refresh Token còn hiệu lực của một người dùng.
        /// </summary>
        /// <param name="userId">ID của người dùng.</param>
        Task RevokeAllForUserAsync(Guid userId);
    }
}
