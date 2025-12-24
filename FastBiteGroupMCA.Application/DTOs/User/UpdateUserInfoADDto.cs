using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class UpdateUserInfoADDto
{
    [Required]
    [StringLength(50)]
    public string FisrtName { get; set; } = string.Empty; // giữ nguyên lỗi chính tả

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
}
