using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User
{
    public class DeactivateUserRequestDto
    {
        [Required]
        public string ReasonCategory { get; set; } // Ví dụ: "Spam", "Harassment"

        public string? ReasonDetails { get; set; } // Chi tiết thêm
        [Required] public byte[] RowVersion { get; set; }
    }
}
