namespace FastBiteGroupMCA.Domain.Entities;

public class PollOptions
{
    public int PollOptionID { get; set; }
    public int PollID { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public virtual Polls? Poll { get; set; }
    public virtual ICollection<PollVotes> Votes { get; set; } = new HashSet<PollVotes>();
}
