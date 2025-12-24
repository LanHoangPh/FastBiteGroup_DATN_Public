using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class UpdateUserBasicInfoRequest
{
    [Required(ErrorMessage = "Họ không được để trống.")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên không được để trống.")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [Required] public byte[] RowVersion { get; set; }
}
