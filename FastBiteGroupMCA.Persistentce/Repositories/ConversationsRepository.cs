using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class ConversationsRepository : GenericRepository<Conversation>, IConversationsRepository
{
    public ConversationsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
