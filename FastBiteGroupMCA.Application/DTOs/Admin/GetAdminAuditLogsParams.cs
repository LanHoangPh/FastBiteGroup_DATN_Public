using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin
{
    public class GetAdminAuditLogsParams : PaginationParams
    {
        public Guid? AdminId { get; set; }
        public EnumAdminActionType? ActionType { get; set; }
        public EnumTargetEntityType? TargetEntityType { get; set; }
        public string? TargetEntityId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? BatchId { get; set; }
        /// <summary>
        /// Độ lệch múi giờ của client so với UTC, tính bằng phút.
        /// Ví dụ: Việt Nam (UTC+7) sẽ là -420.
        /// </summary>
        public int? TimezoneOffsetMinutes { get; set; }
    }
}
