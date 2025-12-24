namespace FastBiteGroupMCA.Application.DTOs.User;

public class LoginHistoryDto
{
    public DateTime LoginTimestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool WasSuccessful { get; set; }
}
