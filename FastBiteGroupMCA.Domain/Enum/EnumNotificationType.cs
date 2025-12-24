namespace FastBiteGroupMCA.Domain.Enum;

public enum EnumNotificationType
{
    NewMessage = 1,
    UserAddedToGroup,
    UserMention,
    NewPostInGroup,
    NewPostComment,
    PostLike,
    NewPoll,
    VideoCallInvitation,
    Deleted,
    AdminPromotion,
    UserKickedFromGroup,
    PostRejected,
    GroupInvitation,
    AccountDeactivated,
    SystemAnnouncement = 99
}
