using FastBiteGroupMCA.Persistentce.Repositories.Efcore;
using System.Linq.Expressions;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class UserGroupInvitationRepository : GenericRepository<UserGroupInvitation>, IUserGroupInvitationRepository
{
    public UserGroupInvitationRepository(ApplicationDbContext context) : base(context)
    {
    }
}
