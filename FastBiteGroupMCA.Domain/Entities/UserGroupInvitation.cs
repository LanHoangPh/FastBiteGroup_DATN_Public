namespace FastBiteGroupMCA.Domain.Entities;

public class UserGroupInvitation
{
    public int InvitationID { get; set; }

    public Guid GroupID { get; set; }
    public virtual Group? Group { get; set; }

    public Guid InvitedUserID { get; set; }
    public virtual AppUser? InvitedUser { get; set; }

    public Guid InvitedByUserID { get; set; }
    public virtual AppUser? InvitedByUser { get; set; }

    public EnumInvitationStatus Status { get; set; } = EnumInvitationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
