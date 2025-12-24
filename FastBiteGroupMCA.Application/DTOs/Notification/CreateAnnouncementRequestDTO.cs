using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Notification;

public class CreateAnnouncementRequestDTO
{
    /// <summary>
    /// Nội dung tóm tắt của thông báo sẽ hiển thị cho người dùng.
    /// </summary>
    /// <example>🔥 Sự kiện đặc biệt: Ra mắt tính năng X sắp diễn ra!</example>
    [Required]
    [StringLength(255)]
    public string ContentPreview { get; set; } = string.Empty;

    /// <summary>
    /// (Tùy chọn) Đường dẫn tương đối trong ứng dụng mà người dùng sẽ được điều hướng đến khi nhấp vào thông báo.
    /// </summary>
    /// <example>/groups/some-group-id/posts/12345</example>
    [StringLength(2048)]
    public string? NavigateUrl { get; set; }
}
