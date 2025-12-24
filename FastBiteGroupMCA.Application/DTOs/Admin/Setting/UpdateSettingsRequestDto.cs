namespace FastBiteGroupMCA.Application.DTOs.Admin.Setting;

public class UpdateSettingsRequestDto
{
    public Dictionary<string, string> Settings { get; set; } = new();
}
