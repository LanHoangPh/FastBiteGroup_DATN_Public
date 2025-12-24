using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.User;

public class AdminUserListItemDTO
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsActive { get; set; }
    public bool IsCurrentUser { get; set; } = false;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // thêm cái này và bên FE ko cần hiển thị
}
