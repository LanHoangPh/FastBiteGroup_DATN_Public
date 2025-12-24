using FastBiteGroupMCA.Domain.Enum;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Group;

public class CreateChatGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EnumGroupType GroupType { get; set; }
    public IFormFile? AvatarFile { get; set; }
    // --- BỔ SUNG THUỘC TÍNH NÀY ---
    /// <summary>
    /// (Tùy chọn) Danh sách ID của những người dùng muốn mời vào ngay lúc tạo.
    /// </summary>
    public List<Guid>? InvitedUserIds { get; set; }
}
