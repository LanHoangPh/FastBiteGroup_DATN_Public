namespace FastBiteGroupMCA.Domain.Enum;

public enum EnumCallSessionStatus
{
    Ringing,    // Đang đổ chuông
    Ongoing,    // Đang diễn ra
    Ended,      // Đã kết thúc (bởi người dùng)
    Missed,     // Bị nhỡ
    Declined,   // Bị từ chối
    Cancelled   // Bị người gọi hủy
}
