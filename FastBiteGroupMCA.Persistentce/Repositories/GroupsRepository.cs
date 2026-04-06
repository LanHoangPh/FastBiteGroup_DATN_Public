using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class GroupsRepository : GenericRepository<Group>, IGroupsRepository
{
    public GroupsRepository(ApplicationDbContext context) : base(context)
    {
    }
    public async Task<bool> SoftDeleteAsync(Guid groupId)
    {
        var group = await _dbSet.FirstOrDefaultAsync(g => g.GroupID == groupId);
        if (group == null || group.IsDeleted) return false;

        group.IsDeleted = true;
        _dbSet.Update(group);
        return true;
    }
    public async Task<Group?> GetByIdGroupAsync(Guid groupId, bool ignoreQueryFilters = false)
    {
        var query = _dbSet.AsQueryable();

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }
        return await query.FirstOrDefaultAsync(g => g.GroupID == groupId);
    }
}
