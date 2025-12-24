using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class PostCommentsRepository : GenericRepository<PostComments>, IPostCommentsRepository
{
    public PostCommentsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
