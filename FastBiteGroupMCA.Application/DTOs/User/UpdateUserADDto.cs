namespace FastBiteGroupMCA.Application.DTOs.User;

public class UpdateUserADDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Bio { get; set; }
    public bool TwoFactorEnabled { get; set; }
}
