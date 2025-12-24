using FastBiteGroupMCA.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class UpdateGroupSettingsDTO
{
    // Sử dụng EnumGroupType để validation tự động
    [Required]
    [EnumDataType(typeof(EnumGroupType))]
    public EnumGroupType GroupType { get; set; }

    // Có thể thêm các settings khác ở đây trong tương lai
    // public bool? IsArchived { get; set; }
}
