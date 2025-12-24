using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class PollVotesRepository : GenericRepository<PollVotes>, IPollVotesRepository
{
    public PollVotesRepository(ApplicationDbContext context) : base(context)
    {
    }
}
