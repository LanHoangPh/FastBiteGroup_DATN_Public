namespace FastBiteGroupMCA.Domain.Entities;

public class Polls : ISoftDelete
{
    public int PollID { get; set; }
    public int ConversationID { get; set; }
    public Guid CreatedByUserID { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool AllowMultipleChoices { get; set; }
    public DateTime? ClosesAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? MessageID { get; set; }
    public bool IsDeleted { get; set; }

    public virtual Conversation? Conversation { get; set; }
    public virtual AppUser? CreatedByUser { get; set; }
    public virtual ICollection<PollOptions> Options { get; set; } = new HashSet<PollOptions>();
}
