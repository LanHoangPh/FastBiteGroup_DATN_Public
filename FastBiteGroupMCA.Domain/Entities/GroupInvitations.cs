using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Domain.Entities;

public class GroupInvitations
{
    public int InvitationID { get; set; }
    public string InvitationCode { get; set; } = string.Empty;
    public Guid GroupID { get; set; }
    public Guid CreatedByUserID { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int CurrentUses { get; set; }
    public bool IsActive { get; set; } = true;
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public virtual Group? Group { get; set; }
    public virtual AppUser? CreatedByUser { get; set; }
}
