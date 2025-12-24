using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FastBiteGroupMCA.Domain.Entities;

/// <summary>
/// Thông tin về tệp đính kèm trong tin nhắn, thay thế bảng MesageFiles
/// </summary>
public class AttachmentInfo
{
    [BsonElement("file_id")]
    public int FileId { get; set; } // Tham chiếu đến FileID trong bảng SharedFiles của SQL

    [BsonElement("file_name")]
    public string FileName { get; set; } = string.Empty; // Tên tệp, ví dụ: "image.png", "document.pdf"...

    [BsonElement("storage_url")]
    public string StorageUrl { get; set; } = string.Empty; // URL lưu trữ tệp

    [BsonElement("file_type")]
    public string FileType { get; set; } = string.Empty; // Loại tệp, ví dụ: "image/png", "application/pdf"...
    [BsonElement("file_size")]
    public long FileSize { get; set; } // Kích thước tệp tính bằng byte
}
