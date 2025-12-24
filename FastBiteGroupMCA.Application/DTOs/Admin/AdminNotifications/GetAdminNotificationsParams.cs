using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.Admin.AdminNotifications;

public class GetAdminNotificationsParams : PaginationParams
{
    public bool? IsRead { get; set; }
    public EnumAdminNotificationType? NotificationType { get; set; }
}
