namespace FastBiteGroupMCA.Domain.Entities;

public class LoginHistory
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime LoginTimestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; } // Chứa thông tin trình duyệt, hệ điều hành
    public bool WasSuccessful { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
