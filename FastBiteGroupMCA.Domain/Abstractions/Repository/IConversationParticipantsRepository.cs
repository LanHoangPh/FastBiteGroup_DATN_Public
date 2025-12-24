namespace FastBiteGroupMCA.Domain.Abstractions.Repository;

public interface IConversationParticipantsRepository : IGenericRepository<ConversationParticipants>
{
    Task<Conversation> GetConversationByParticipantIdAsync(int participantId);
}
