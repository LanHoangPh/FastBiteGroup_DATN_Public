using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class GroupForList_AdminDto
{
    public Guid GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? CreatorName { get; set; }
    public int MemberCount { get; set; }
    public int PostCount { get; set; } // Thêm
    public GroupTypeApiDto GroupType { get; set; }
    public EnumGroupPrivacy Privacy { get; set; }
    public bool IsDeleted { get; set; } // Thêm, để đánh dấu nhóm đã bị xóa mềm
    public bool IsArchived { get; set; } // Thêm
    public DateTime CreatedAt { get; set; }
    // --- Thông tin bổ sung để "chẩn đoán---
    public DateTime? LastActivityAt { get; set; } // Thời gian hoạt động gần nhất
    public int PendingReportsCount { get; set; }  // Số báo cáo đang chờ xử lý 🚩
}
