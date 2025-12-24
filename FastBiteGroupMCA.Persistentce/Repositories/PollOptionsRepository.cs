using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class PollOptionsRepository : GenericRepository<PollOptions>, IPollOptionsRepository
{
    public PollOptionsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
