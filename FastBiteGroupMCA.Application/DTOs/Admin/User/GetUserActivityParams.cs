using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class GetUserActivityParams : PaginationParams
{
    // Thêm các thuộc tính filter vào đây
    // THAY ĐỔI: Chuyển từ string? sang EnumUserActivityType?
    // Dùng kiểu nullable (Enum?) để khi client không truyền tham số này, 
    // giá trị của nó sẽ là null, có nghĩa là "lấy tất cả".
    public EnumUserActivityType? ActivityType { get; set; }
    public Guid? GroupId { get; set; } // Filter theo một nhóm cụ thể
}
