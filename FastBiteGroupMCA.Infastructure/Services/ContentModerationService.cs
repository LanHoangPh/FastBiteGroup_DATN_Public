using Azure.AI.ContentSafety;
using Azure;
using System.Text.Json.Nodes;
using System.Text;
using System.Text.Json;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Notifications.Templates;

namespace FastBiteGroupMCA.Infastructure.Services;

public class ContentModerationService : IContentModerationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ContentSafetyClient _contentSafetyClient;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService; 
    private readonly ILogger<ContentModerationService> _logger;

    public ContentModerationService(IUnitOfWork unitOfWork, ContentSafetyClient contentSafetyClient, ILogger<ContentModerationService> logger, ISettingsService settingsService, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _contentSafetyClient = contentSafetyClient;
        _logger = logger;
        _settingsService = settingsService;
        _notificationService = notificationService;
    }

    public async Task ModeratePostAsync(int postId)
    {
        var postData = await _unitOfWork.Posts.GetQueryable()
            .Where(p => p.PostID == postId && p.Status == EnumPostStatus.PendingReview)
            .Select(p => new
            {
                Post = p,
                AttachmentUrls = p.Attachments.Where(a => a.SharedFile != null && a.SharedFile.FileType.StartsWith("image/"))
                                            .Select(a => a.SharedFile.StorageUrl)
                                            .ToList()
            })
            .FirstOrDefaultAsync();

        if (postData == null) return;

        var post = postData.Post;
        string plainText = ExtractTextFromJson(post.ContentJson);
        bool isViolated = false;
        string rejectionReason = "";

        var forbiddenKeywordsCsv = _settingsService.Get<string>(SettingKeys.ForbiddenKeywords, "");
        if (!string.IsNullOrEmpty(forbiddenKeywordsCsv))
        {
            var forbiddenKeywords = forbiddenKeywordsCsv.Split(',').Select(k => k.Trim().ToLower());
            if (forbiddenKeywords.Any(keyword => plainText.ToLower().Contains(keyword)))
            {
                isViolated = true;
                rejectionReason = "Nội dung chứa từ khóa không phù hợp.";
            }
        }
        if (!isViolated)
        {
            if (!string.IsNullOrWhiteSpace(plainText))
            {
                try
                {
                    var request = new AnalyzeTextOptions(plainText);
                    var response = await _contentSafetyClient.AnalyzeTextAsync(request);
                    if (response.Value.CategoriesAnalysis.Any(c => c.Severity > 1))
                    {
                        isViolated = true;
                        rejectionReason = "Nội dung văn bản vi phạm chính sách cộng đồng.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analyzing text for post {PostId}", postId);
                }
            }
            if (!isViolated && postData.AttachmentUrls.Any())
            {
                foreach (var imageUrl in postData.AttachmentUrls)
                {
                    try
                    {
                        var imageRequest = new AnalyzeImageOptions(new ContentSafetyImageData(new Uri(imageUrl)));
                        var response = await _contentSafetyClient.AnalyzeImageAsync(imageRequest);
                        if (response.Value.CategoriesAnalysis.Any(c => c.Severity > 1))
                        {
                            isViolated = true;
                            rejectionReason = "Nội dung hình ảnh vi phạm chính sách cộng đồng.";
                            break; 
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error analyzing image for post {PostId}", postId);
                    }
                }
            }
        }

        // --- 3. CẬP NHẬT TRẠNG THÁI BÀI VIẾT ---
        post.Status = isViolated ? EnumPostStatus.Rejected : EnumPostStatus.Published;
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Moderation completed for PostId {PostId}. Result: {Status}", postId, post.Status);

        if (isViolated)
        {
            var eventData = new PostRejectedEventData(post, rejectionReason);
            await _notificationService.DispatchNotificationAsync<PostRejectedNotificationTemplate, PostRejectedEventData>(
                post.AuthorUserID, // Gửi cho tác giả bài viết
                eventData
            );
        }
    }

    /// <summary>
    /// Trích xuất toàn bộ văn bản thuần túy từ một chuỗi JSON của Tiptap/ProseMirror
    /// bằng cách duyệt đệ quy qua cấu trúc cây.
    /// </summary>
    private string ExtractTextFromJson(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        try
        {
            var jsonNode = JsonNode.Parse(jsonContent);
            if (jsonNode != null)
            {
                // Bắt đầu quá trình duyệt đệ quy từ node gốc
                TraverseNode(jsonNode, sb);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Không thể parse JSON nội dung bài viết để trích xuất text.");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Hàm đệ quy để duyệt qua một node và các node con của nó.
    /// </summary>
    private void TraverseNode(JsonNode node, StringBuilder sb)
    {
        if (node == null) return;

        // 1. Nếu node này là một node văn bản, lấy text của nó
        if (node["type"]?.GetValue<string>() == "text" && node["text"] is JsonNode textNode)
        {
            sb.Append(textNode.GetValue<string>() + " ");
        }

        // 2. Duyệt qua tất cả các node con trong mảng "content" (nếu có)
        if (node["content"] is JsonArray contentArray)
        {
            foreach (var childNode in contentArray)
            {
                if (childNode != null)
                {
                    // Gọi lại chính nó cho từng node con
                    TraverseNode(childNode, sb);
                }
            }
        }
    }
}
