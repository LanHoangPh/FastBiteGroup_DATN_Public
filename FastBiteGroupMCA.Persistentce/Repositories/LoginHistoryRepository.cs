using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class LoginHistoryRepository : GenericRepository<LoginHistory>, ILoginHistoryRepository
{
    public LoginHistoryRepository(ApplicationDbContext context) : base(context)
    {
    }
}
