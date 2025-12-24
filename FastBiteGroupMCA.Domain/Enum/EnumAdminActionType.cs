namespace FastBiteGroupMCA.Domain.Enum;

public enum EnumAdminActionType
{
    // User Actions
    UserDeactivated,
    UserReactivated,
    UserPasswordForcedReset,

    // Group Actions
    GroupCreated,
    GroupArchived,
    GroupUnarchived,
    GroupSoftDeleted,
    GroupOwnerChanged,
    GroupSettingsChanged,
    GroupMemberRoleChanged,
    GroupMemberRemoved,
    GroupUpdated,
    GroupMemberAddedBySystem,
    GroupRestored,
    GroupAvatarUpdated,

    // Content Actions
    PostSoftDeleted,
    PostRestored,
    CommentDeletedByAdmin,

    // User Management Actions
    UserSoftDeleted,
    UserRestored,
    UserCreatedByAdmin,
    UserUpdated,
    UserBioRemoved,
    UserAvatarRemoved,
    UserPasswordResetForced,
    UserRoleAssigned,
    UserRoleRemoved,

    // Role admin 
    RoleCreated,
    RoleUpdated,
    RoleDeleted,

    // System Actions
    SettingsUpdated
}
