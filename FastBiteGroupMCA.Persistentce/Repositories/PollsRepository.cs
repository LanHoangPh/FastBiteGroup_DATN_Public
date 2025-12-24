using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class PollsRepository : GenericRepository<Polls>, IPollsRepository
{
    public PollsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
