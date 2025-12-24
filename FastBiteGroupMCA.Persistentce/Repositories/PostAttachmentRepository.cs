using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class PostAttachmentRepository : GenericRepository<PostAttachment>, IPostAttachmentRepository
{
    public PostAttachmentRepository(ApplicationDbContext context) : base(context)
    {
    }
}
