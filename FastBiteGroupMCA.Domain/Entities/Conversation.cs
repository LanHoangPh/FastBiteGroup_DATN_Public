using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Domain.Entities;

public class Conversation : IDateTracking, ISoftDelete
{
    public int ConversationID { get; set; }
    public EnumConversationType ConversationType { get; set; } 
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }

    public Guid? ExplicitGroupID { get; set; }
    public bool IsDeleted { get; set; }
    public virtual Group? Group { get; set; }

    [MaxLength(200)]
    public string? LastMessagePreview { get; set; } // Nội dung xem trước ("Đã gửi một ảnh", "Hello world"...)
    public DateTime? LastMessageTimestamp { get; set; } // Thời gian của tin nhắn cuối
    [MaxLength(100)]
    public string? LastMessageSenderName { get; set; } // Tên người gửi tin nhắn cuối
    public virtual ICollection<ConversationParticipants> Participants { get; set; } = new HashSet<ConversationParticipants>();
    public DateTime CreatedAt { get ; set ; }
    public DateTime? UpdatedAt { get ; set ; }
}
