using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class MyProfileDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
    // --- BỔ SUNG THEO GỢI Ý CỦA BẠN ---
    /// <summary>
    /// Trạng thái online/offline real-time của người dùng.
    /// </summary>
    public EnumUserPresenceStatus PresenceStatus { get; set; }

    /// <summary>
    /// Cài đặt quyền riêng tư về tin nhắn của người dùng.
    /// </summary>
    public EnumMessagingPrivacy MessagingPrivacy { get; set; }
    public List<MyGroupInfoDto> Groups { get; set; } = new();
    public List<MyPostInfoDto> RecentPosts { get; set; } = new();
}
public class MyGroupInfoDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? GroupAvatarUrl { get; set; }
}
public class MyPostInfoDto
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
