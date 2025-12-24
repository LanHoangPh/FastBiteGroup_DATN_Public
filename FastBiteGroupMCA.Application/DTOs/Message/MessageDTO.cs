using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Message;

public class MessageDTO
{
    /// <summary>
    /// ID của tin nhắn (từ MongoDB).
    /// </summary>
    /// <example>66b72a3b1a2345b678c9d0e1</example>
    public string Id { get; set; } = string.Empty;
    public int ConversationId { get; set; }

    /// <summary>Thông tin người gửi.</summary>
    public MessageSenderDTO Sender { get; set; } = null!;

    /// <summary>Nội dung tin nhắn (nếu là tin nhắn văn bản).</summary>
    public string Content { get; set; } = string.Empty;
    public EnumMessageType MessageType { get; set; }

    /// <summary>Thời gian gửi.</summary>
    public DateTime SentAt { get; set; }

    /// <summary>Tin nhắn đã bị thu hồi hay chưa.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Danh sách các tệp đính kèm (nếu có).</summary>
    public List<AttachmentInfo>? Attachments { get; set; }

    /// <summary>Danh sách các biểu cảm trên tin nhắn (nếu có).</summary>
    public List<ReactionDto>? Reactions { get; set; }

    /// <summary>ID của tin nhắn cha (nếu đây là tin nhắn trả lời).</summary>
    public string? ParentMessageId { get; set; }

    /// <summary>Thông tin xem trước của tin nhắn cha (nếu đây là tin nhắn trả lời).</summary>
    public ParentMessageInfo? ParentMessage { get; set; }
    public List<ReadReceiptDto>? ReadBy { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool IsMine { get; set; }

    public EnumGroupRole? SenderRoleInGroup { get; set; } 
}
