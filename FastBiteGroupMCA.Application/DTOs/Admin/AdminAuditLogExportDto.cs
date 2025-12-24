using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.Admin
{
    public class AdminAuditLogExportDto
    {
        [Display(Name = "ID Log")]
        public long Id { get; set; }

        [Display(Name = "Admin Thực hiện")]
        public string AdminFullName { get; set; } = string.Empty;

        [Display(Name = "Loại Hành động")]
        public string ActionType { get; set; } = string.Empty; // Dùng string cho dễ đọc

        [Display(Name = "Loại Đối tượng")]
        public string TargetEntityType { get; set; } = string.Empty; // Dùng string

        [Display(Name = "ID Đối tượng")]
        public string TargetEntityId { get; set; } = string.Empty;

        [Display(Name = "Chi tiết")]
        public string? Details { get; set; }

        [Display(Name = "Thời gian (Giờ địa phương)")]
        public DateTime Timestamp { get; set; } // Sẽ chứa giờ đã được chuyển đổi

        [Display(Name = "ID Lô")]
        public Guid? BatchId { get; set; }
    }
}
