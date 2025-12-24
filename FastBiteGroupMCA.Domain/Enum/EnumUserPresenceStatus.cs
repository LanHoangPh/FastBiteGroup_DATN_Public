using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Domain.Enum;

public enum EnumUserPresenceStatus
{
    [Display(Name = "Trực tuyến")]
    Online = 1,

    [Display(Name = "Vắng mặt")]
    Absent,

    [Display(Name = "Đang bận")]
    Busy,

    [Display(Name = "Ngoại tuyến")]
    Offline
}
