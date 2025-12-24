using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Domain.Entities.Identity;

public class AppUser : IdentityUser<Guid>, IDateTracking, ISoftDelete
{
    [Required]
    public string FisrtName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    [Required]
    [StringLength(255)]
    public string? FullName { get; set; }
    [StringLength(2048)]
    public string? AvatarUrl { get; set; }
    [StringLength(500)]
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public EnumUserPresenceStatus PresenceStatus { get; set; } = EnumUserPresenceStatus.Offline;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public EnumMessagingPrivacy MessagingPrivacy { get; set; } = EnumMessagingPrivacy.FromSharedGroupMembers;
    [StringLength(50)]
    public string? OneSignalPlayerId { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }

    // Mối quan hệ với các bảng do user này "sở hữu" hoặc "khởi tạo"
    public virtual ICollection<Group> CreatedGroups { get; set; }
    public virtual ICollection<Posts> AuthoredPosts { get; set; }
    public virtual ICollection<Polls> CreatedPolls { get; set; }
    public virtual ICollection<VideoCallSessions> InitiatedVideoCallSessions { get; set; }
    public virtual ICollection<SharedFiles> UploadedFiles { get; set; }
    public virtual ICollection<GroupInvitations> CreatedGroupInvitations { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();

    // Mối quan hệ qua các bảng trung gian (many-to-many)
    public virtual ICollection<GroupMember> GroupMemberships { get; set; }
    public virtual ICollection<ConversationParticipants> ConversationParticipations { get; set; }
    public virtual ICollection<VideoCallParticipants> VideoCallParticipations { get; set; }

    // Mối quan hệ với các hành động của user
    public virtual ICollection<PostComments> PostComments { get; set; }
    public virtual ICollection<PostLikes> PostLikes { get; set; }
    public virtual ICollection<PollVotes> PollVotes { get; set; }
    public virtual ICollection<UserGroupInvitation> ReceivedInvitations { get; set; }
    public virtual ICollection<ContentReport> ContentReports { get; set; } = new HashSet<ContentReport>();

    // Tập hợp các lời mời mà user này ĐÃ GỬI ĐI
    public virtual ICollection<UserGroupInvitation> SentInvitations { get; set; }

    // Constructor để khởi tạo các collection, tránh lỗi NullReferenceException
    public AppUser()
    {
        CreatedGroups = new HashSet<Group>();
        AuthoredPosts = new HashSet<Posts>();
        CreatedPolls = new HashSet<Polls>();
        InitiatedVideoCallSessions = new HashSet<VideoCallSessions>();
        UploadedFiles = new HashSet<SharedFiles>();
        CreatedGroupInvitations = new HashSet<GroupInvitations>();
        GroupMemberships = new HashSet<GroupMember>();
        ConversationParticipations = new HashSet<ConversationParticipants>();
        VideoCallParticipations = new HashSet<VideoCallParticipants>();
        PostComments = new HashSet<PostComments>();
        PostLikes = new HashSet<PostLikes>();
        PollVotes = new HashSet<PollVotes>();
        ReceivedInvitations = new HashSet<UserGroupInvitation>();
        SentInvitations = new HashSet<UserGroupInvitation>();
        ContentReports = new HashSet<ContentReport>();
        RowVersion = Array.Empty<byte>(); // Initialize RowVersion to avoid nullability issues
    }
}
