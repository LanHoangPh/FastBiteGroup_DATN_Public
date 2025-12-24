namespace FastBiteGroupMCA.Application.DTOs.Message;

public class ReactionDto
{
    public Guid UserId { get; set; }
    public string ReactionCode { get; set; } = string.Empty;
    // --- BỔ SUNG 2 TRƯỜNG NÀY ---
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
