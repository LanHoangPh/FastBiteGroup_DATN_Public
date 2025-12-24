using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Group
{
    public class UpdateGroupRequestDTO
    {
        [Required(ErrorMessage = "Tên nhóm không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên nhóm không được vượt quá 100 ký tự.")]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }
    }
}
