using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class PostsRepository : GenericRepository<Posts>, IPostsRepository
{
    public PostsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
