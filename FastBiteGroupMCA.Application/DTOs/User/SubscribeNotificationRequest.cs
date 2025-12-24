using System.ComponentModel.DataAnnotations;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class SubscribeNotificationRequest
{
    [Required]
    public string PlayerId { get; set; } = string.Empty;
}
