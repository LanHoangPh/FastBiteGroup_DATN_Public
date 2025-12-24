using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class GroupInvitationsRepository : GenericRepository<GroupInvitations>, IGroupInvitationsRepository
{
    public GroupInvitationsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
