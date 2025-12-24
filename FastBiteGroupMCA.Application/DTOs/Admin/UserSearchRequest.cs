using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Admin;

public class UserSearchRequest : PaginationParams
{
    /// <summary>
    /// Từ khóa tìm kiếm theo Tên hiển thị, Username hoặc Email.
    /// </summary>
    public string? Query { get; set; } // << ĐẢM BẢO 100% CÓ { get; set; }

    /// <summary>
    /// (Tùy chọn) ID của một nhóm để loại trừ các thành viên đã có trong nhóm đó.
    /// </summary>
    public Guid? ExcludeGroupId { get; set; }
}
