using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class GroupMembersRepository : GenericRepository<GroupMember>, IGroupMembersRepository
{
    public GroupMembersRepository(ApplicationDbContext context) : base(context)
    {
    }
}
