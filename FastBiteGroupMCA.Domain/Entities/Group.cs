using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Domain.Entities;

public class Group : IDateTracking, ISoftDelete
{
    public Guid GroupID { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EnumGroupType GroupType { get; set; } = EnumGroupType.Public; 
    public EnumGroupPrivacy Privacy { get; set; } = EnumGroupPrivacy.Public;
    public string GroupAvatarUrl { get; set; }= string.Empty;
    public bool IsArchived { get; set; } // trạng thái nhóm có bị lưu trữ hay không
    public Guid CreatedByUserID { get; set; }
    public virtual AppUser? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int? ConversationId { get; set; }
    public virtual Conversation? Conversation { get; set; }

    public virtual ICollection<GroupMember> Members { get; set; } = new HashSet<GroupMember>();
    public virtual ICollection<Posts> Posts { get; set; } = new HashSet<Posts>();
    public virtual ICollection<GroupInvitations> GroupInvitations { get; set; } = new HashSet<GroupInvitations>();
    public virtual ICollection<UserGroupInvitation> DirectUserInvitations { get; set; } = new HashSet<UserGroupInvitation>();
    public virtual ICollection<ContentReport> ContentReports { get; set; } = new HashSet<ContentReport>();
}
