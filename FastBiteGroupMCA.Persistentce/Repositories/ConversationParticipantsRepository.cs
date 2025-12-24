using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class ConversationParticipantsRepository : GenericRepository<ConversationParticipants>, IConversationParticipantsRepository
{
    public ConversationParticipantsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Conversation> GetConversationByParticipantIdAsync(int participantId)
    {
        throw new NotImplementedException();
    }
}
