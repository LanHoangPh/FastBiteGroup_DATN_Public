using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastBiteGroupMCA.Domain.Abstractions.Repository
{
    public interface IContentReportsRepository : IGenericRepository<ContentReport>
    {
        Task<bool> ExistsReportAsync(int contentId, Guid userId, EnumReportedContentType contentType);
    }
}
