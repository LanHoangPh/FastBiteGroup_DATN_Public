using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class PostLikesRepository : GenericRepository<PostLikes>, IPostLikesRepository
{
    public PostLikesRepository(ApplicationDbContext context) : base(context)
    {
    }
}