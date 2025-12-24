using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class AdminNotificationsRepository : GenericRepository<AdminNotifications>, IAdminNotificationsRepository
{
    public AdminNotificationsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
