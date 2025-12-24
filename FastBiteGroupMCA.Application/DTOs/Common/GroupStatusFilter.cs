namespace FastBiteGroupMCA.Application.DTOs.Common;

public enum GroupStatusFilter
{
    Active = 1,   // Mặc định: Đang hoạt động (!IsDeleted && !IsArchived)
    Archived,     // Đã lưu trữ (!IsDeleted && IsArchived)
    Deleted,       // Đã xóa (IsDeleted)
    All // Mới
}
