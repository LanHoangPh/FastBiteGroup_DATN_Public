namespace FastBiteGroupMCA.Application.DTOs.Message;

public class ReadReceiptDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime ReadAt { get; set; }
}
