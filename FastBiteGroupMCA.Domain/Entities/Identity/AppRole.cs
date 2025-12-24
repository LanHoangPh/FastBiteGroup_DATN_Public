using Microsoft.AspNetCore.Identity;

namespace FastBiteGroupMCA.Domain.Entities.Identity;

public class AppRole : IdentityRole<Guid>
{
    // THÊM THUỘC TÍNH NÀY
    /// <summary>
    /// Đánh dấu đây có phải là một vai trò cốt lõi của hệ thống hay không.
    /// Vai trò hệ thống không thể bị sửa hoặc xóa bởi Admin.
    /// </summary>
    public bool IsSystemRole { get; set; }
}
