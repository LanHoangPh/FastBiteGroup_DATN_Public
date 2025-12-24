namespace FastBiteGroupMCA.Domain.Enum;

public enum EnumPostStatus
{
    PendingReview,      // Đang chờ duyệt (trạng thái ban đầu)
    Published,          // Đã được duyệt và hiển thị công khai
    NeedsModeratorReview, // AI không chắc chắn, cần người duyệt
    Rejected            // Bị từ chối do vi phạm
}
