using Ganss.Xss;
using System.Text.Json.Nodes;
using System.Text;
using System.Text.Json;

namespace FastBiteGroupMCA.Infastructure.Services;

public class ContentRendererService : IContentRendererService
{
    private readonly HtmlSanitizer _sanitizer;

    public ContentRendererService()
    {
        // Cấu hình sanitizer để cho phép các thẻ và thuộc tính cơ bản
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("u");
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("a");
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("rel");
        _sanitizer.AllowedCssProperties.Add("color");
        _sanitizer.AllowedCssProperties.Add("background-color");
        _sanitizer.AllowedCssProperties.Add("font-size");
    }

    public string RenderAndSanitize(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return string.Empty;
        }

        try
        {
            // Đây là một trình render đơn giản. Có thể mở rộng để xử lý mentions, video...
            var jsonNode = JsonNode.Parse(jsonContent);
            var html = ProcessNode(jsonNode);
            return _sanitizer.Sanitize(html);
        }
        catch (JsonException)
        {
            // Nếu JSON không hợp lệ, trả về chuỗi rỗng
            return string.Empty;
        }
    }

    private string ProcessNode(JsonNode node)
    {
        if (node is null) return string.Empty;

        var type = node["type"]?.GetValue<string>();
        var content = new StringBuilder();

        if (node["content"] is JsonArray contentArray)
        {
            foreach (var childNode in contentArray)
            {
                content.Append(ProcessNode(childNode!));
            }
        }
        else if (node["text"] is JsonNode textNode)
        {
            content.Append(System.Net.WebUtility.HtmlEncode(textNode.GetValue<string>()));
        }

        return type switch
        {
            "doc" => $"<div>{content}</div>",
            "paragraph" => $"<p>{content}</p>",
            "bold" => $"<strong>{content}</strong>",
            "italic" => $"<em>{content}</em>",
            "underline" => $"<u>{content}</u>",
            _ => content.ToString()
        };
    }
}
