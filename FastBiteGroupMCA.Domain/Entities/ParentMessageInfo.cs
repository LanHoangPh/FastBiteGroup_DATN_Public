using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FastBiteGroupMCA.Domain.Entities;

/// <summary>
/// lưu thôn gngyười gửi tn 
/// </summary>
public class ParentMessageInfo
{ 
    [BsonElement("sender_name")]
    public string SenderName { get; set; } = string.Empty; // Tên người gửi tin nhắn gốc

    [BsonElement("content_snippet")]
    public string ContentSnippet { get; set; } = string.Empty; // Mô tả ngắn gọn nội dung của tin nhắn gốc
}