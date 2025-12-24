namespace FastBiteGroupMCA.Domain.Enum;

public enum EnumMessagingPrivacy
{
    // Chỉ nhận tin nhắn từ những người có chung ít nhất 1 nhóm
    FromSharedGroupMembers = 1,

    // Nhận tin nhắn từ bất kỳ ai trong hệ thống
    FromAnyone = 2
}
