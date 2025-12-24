using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using System.Data;
using System.Linq.Expressions;

namespace FastBiteGroupMCA.Persistentce.Repositories.Efcore;
public class GenericRepository<TEnity> : IGenericRepository<TEnity> where TEnity : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<TEnity> _dbSet;

    protected readonly IDbConnection _dbConnection;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEnity>();
        // Lấy kết nối DB từ DbContext để dùng chung cho cá tác vị dapper sau này
        _dbConnection = context.Database.GetDbConnection();
    }
    public virtual async Task AddAsync(TEnity entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEnity> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public virtual async Task<IEnumerable<TEnity>> FindAsync(Expression<Func<TEnity, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<IEnumerable<TEnity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<TEnity?> GetByIdAsync(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    public IQueryable<TEnity> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public virtual void Remove(TEnity entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEnity> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public virtual void Update(TEnity entity)
    {
        _dbSet.Update(entity);
    }
}
