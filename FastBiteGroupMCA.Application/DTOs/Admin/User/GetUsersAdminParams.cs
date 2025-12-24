using FastBiteGroupMCA.Application.DTOs.Common;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class GetUsersAdminParams : PaginationParams
{
    /// <summary>
    /// Tìm kiếm theo tên hoặc email.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Lọc theo vai trò (ví dụ: "Admin", "Member").
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Lọc theo trạng thái tài khoản (true = active, false = deactivated).
    /// </summary>
    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }
    // THÊM THUỘC TÍNH NÀY
    /// <summary>
    /// Độ lệch múi giờ của client so với UTC, tính bằng phút.
    /// Ví dụ: Việt Nam (UTC+7) sẽ là -420.
    /// </summary>
    public int? TimezoneOffsetMinutes { get; set; }
}
