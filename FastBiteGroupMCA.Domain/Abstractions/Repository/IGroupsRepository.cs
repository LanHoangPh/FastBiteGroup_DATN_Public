namespace FastBiteGroupMCA.Domain.Abstractions.Repository;

public interface IGroupsRepository : IGenericRepository<Group>
{
    Task<bool> SoftDeleteAsync(Guid groupId);
    Task<Group?> GetByIdGroupAsync(Guid groupId, bool ignoreQueryFilters = false);
}
