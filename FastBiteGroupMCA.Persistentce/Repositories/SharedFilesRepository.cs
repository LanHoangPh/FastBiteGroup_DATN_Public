using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class SharedFilesRepository : GenericRepository<SharedFiles>, ISharedFilesRepository
{
    public SharedFilesRepository(ApplicationDbContext context) : base(context)
    {
    }
}
