using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.DTOs.User;

public class UpdatePrivacySettingsDto
{
    public EnumMessagingPrivacy MessagingPrivacy { get; set; }
}
