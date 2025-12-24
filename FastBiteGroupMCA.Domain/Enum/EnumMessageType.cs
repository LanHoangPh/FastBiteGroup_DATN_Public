namespace FastBiteGroupMCA.Domain.Enum;

public enum EnumMessageType
{
    Text = 1,               // Tin nhắn văn bản
    Image,              // Tin nhắn hình ảnh
    File,               // Tin nhắn tệp đính kèm
    Video,              // Tin nhắn video
    Audio,              // Tin nhắn âm thanh
    Poll,               // Tin nhắn chứa bình chọn
    VideoCall,          // Tin nhắn thông báo về cuộc gọi
    SystemNotification,  // Tin nhắn từ hệ thống (VD: A đã tham gia nhóm)
    Delete               // dùng cho thuhồi tin nhắn, đánh dấu tin nhắn đã bị xóa
}
