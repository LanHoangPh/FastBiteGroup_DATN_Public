using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class GlobalSettingsRepository : GenericRepository<GlobalSettings>, IGlobalSettingsRepository
{
    public GlobalSettingsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
