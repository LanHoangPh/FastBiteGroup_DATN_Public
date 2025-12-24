    using Microsoft.AspNetCore.Http;

namespace FastBiteGroupMCA.Application.DTOs.User;

/// <summary>
/// DTO cho Admin cập nhật họ tên và avatar của người dùng (hậu tố AD).
/// </summary>
public class UpdateUserInfoAvatarAdDto
{
    /// <summary>
    /// Tên (bị sai chính tả cố ý để đồng bộ DB).
    /// </summary>
    public string FisrtName { get; set; } = string.Empty;

    /// <summary>
    /// Họ.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// File ảnh avatar. Có thể null nếu chỉ cập nhật tên.
    /// </summary>
    public IFormFile? Avatar { get; set; }
}
