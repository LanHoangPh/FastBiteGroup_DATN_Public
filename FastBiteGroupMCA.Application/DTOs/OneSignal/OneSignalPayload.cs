using System.Text.Json.Serialization;

namespace FastBiteGroupMCA.Application.DTOs.OneSignal;

public class OneSignalPayload
{
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("include_player_ids")]
    public List<string> IncludePlayerIds { get; set; } = new List<string>();

    [JsonPropertyName("contents")]
    public Dictionary<string, string> Contents { get; set; } = new Dictionary<string, string>();

    [JsonPropertyName("headings")]
    public Dictionary<string, string> Headings { get; set; } = new Dictionary<string, string>();

    // Dễ dàng thêm các thuộc tính khác sau này
    [JsonPropertyName("web_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Bỏ qua nếu null
    public string? WebUrl { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}
