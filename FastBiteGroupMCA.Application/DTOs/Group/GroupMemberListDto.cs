using System.Text.Json.Serialization;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Group
{
    /// <summary>
    /// DTO trả về trong API danh sách thành viên (US-CUS-24).
    /// Enum được serialize dưới dạng chuỗi.
    /// </summary>
    public sealed class GroupMemberListDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public EnumGroupRole Role { get; set; }

        public EnumUserPresenceStatus PresenceStatus { get; set; }

        public DateTime JoinedAt { get; set; }
        // --- BỔ SUNG CÁC CỜ QUYỀN HẠN ---
        /// <summary>
        /// Người dùng hiện tại có quyền thay đổi vai trò của thành viên này không?
        /// </summary>
        public bool CanManageRole { get; set; }

        /// <summary>
        /// Người dùng hiện tại có quyền xóa thành viên này khỏi nhóm không?
        /// </summary>
        public bool CanKick { get; set; }
    }
}
