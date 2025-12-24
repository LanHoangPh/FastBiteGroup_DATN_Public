namespace FastBiteGroupMCA.Domain.Entities;

public class PollVotes
{
    public long PollVoteID { get; set; }
    public int PollOptionID { get; set; }
    public Guid UserID { get; set; }
    public DateTime VotedAt { get; set; }
    public virtual PollOptions? PollOption { get; set; }
    public virtual AppUser? User { get; set; }
}
