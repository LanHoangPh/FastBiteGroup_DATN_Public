
namespace FastBiteGroupMCA.Domain.Entities;

public class RefreshToken : IDateTracking
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public virtual AppUser? User { get; set; } 
}
