using FastBiteGroupMCA.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User
{
    public class BulkUserActionRequest
    {
        [Required]
        public EnumBulkUserActionType Action { get; set; }

        [Required]
        [MinLength(1)]
        // THAY ĐỔI TỪ List<Guid> thành List<UserWithVersionDto>
        public List<UserWithVersionDto> Users { get; set; } = new();

        /// <summary>
        /// Tên vai trò cần gán/xóa. Bắt buộc khi Action là AssignRole hoặc RemoveRole.
        /// </summary>
        public string? RoleName { get; set; }

        /// <summary>
        /// Lý do thực hiện hành động. Bắt buộc khi Action là Deactivate.
        /// </summary>
        public string? ReasonCategory { get; set; } // Ví dụ: "Spam", "Harassment"

        public string? ReasonDetails { get; set; } // Chi tiết thêm
    }
    public class UserWithVersionDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public byte[] RowVersion { get; set; }
    }
}
