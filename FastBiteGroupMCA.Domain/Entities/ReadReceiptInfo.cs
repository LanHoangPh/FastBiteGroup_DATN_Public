using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using FastBiteGroupMCA.Domain.Attributes;

namespace FastBiteGroupMCA.Domain.Entities;

// Thông tin về người đã đọc tin nhắn, thay thế bảng MessageReadStatus
//[BsonCollection("read_receipts")]
public class ReadReceiptInfo
{
    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonElement("read_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReadAt { get; set; }
}