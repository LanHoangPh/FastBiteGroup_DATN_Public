using FastBiteGroupMCA.Domain.Enum;
using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class ContentReportsRepository : GenericRepository<ContentReport>, IContentReportsRepository
{
    public ContentReportsRepository(ApplicationDbContext context) : base(context)
    {
    }
    public async Task<bool> ExistsReportAsync(int contentId, Guid userId, EnumReportedContentType contentType)
    {
        return await _context.ContentReports
            .AnyAsync(r => r.ReportedContentID == contentId &&
                           r.ReportedByUserID == userId &&
                           r.ReportedContentType == contentType);
    }
}
