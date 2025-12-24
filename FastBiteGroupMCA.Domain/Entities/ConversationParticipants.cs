namespace FastBiteGroupMCA.Domain.Entities;

public class ConversationParticipants
{
    public int ConversationParticipantID { get; set; }
    public int ConversationID { get; set; }
    public Guid UserID { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastReadTimestamp { get; set; }
    public bool IsMuted { get; set; }
    public bool IsArchived { get; set; }
  
    public virtual Conversation? Conversation { get; set; }
    public virtual AppUser? User { get; set; }
}
