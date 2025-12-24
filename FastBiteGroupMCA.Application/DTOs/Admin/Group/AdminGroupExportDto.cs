using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin.Group;

public class AdminGroupExportDto
{
    [Display(Name = "ID Nhóm")]
    public Guid GroupId { get; set; }
    [Display(Name = "Tên Nhóm")]
    public string GroupName { get; set; }
    [Display(Name = "Người tạo")]
    public string CreatorName { get; set; }
    [Display(Name = "Số thành viên")]
    public int MemberCount { get; set; }
    [Display(Name = "Loại nhóm")]
    public string GroupType { get; set; }
    [Display(Name = "Trạng thái")]
    public string Status { get; set; }
    [Display(Name = "Ngày tạo")]
    public DateTime CreatedAt { get; set; }
}
