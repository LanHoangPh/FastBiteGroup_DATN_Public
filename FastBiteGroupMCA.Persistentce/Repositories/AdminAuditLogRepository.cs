using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class AdminAuditLogRepository : GenericRepository<AdminAuditLog>, IAdminAuditLogRepository
{
    public AdminAuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }
}
