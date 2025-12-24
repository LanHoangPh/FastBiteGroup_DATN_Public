using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Invitation
{
    public class SentGroupInvitationDTO
    {
        public int InvitationId { get; set; }
        public EnumInvitationStatus Status { get; set; }
        public DateTime InvitedAt { get; set; }

        // Thông tin người được mời
        public Guid InvitedUserId { get; set; }
        public string InvitedUserFullName { get; set; } = string.Empty;
        public string? InvitedUserAvatarUrl { get; set; }

        // Thông tin người gửi lời mời
        public Guid InvitedByUserId { get; set; }
        public string InvitedByFullName { get; set; } = string.Empty;
    }
}
