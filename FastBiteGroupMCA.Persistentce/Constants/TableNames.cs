namespace FastBiteGroupMCA.Persistentce.Constants
{
    internal static class TableNames
    {
        // Identity Tables
        internal const string Users = nameof(Users);
        internal const string Roles = nameof(Roles);
        internal const string UserRoles = nameof(UserRoles);
        internal const string UserClaims = nameof(UserClaims);
        internal const string UserLogins = nameof(UserLogins);
        internal const string RoleClaims = nameof(RoleClaims);
        internal const string UserTokens = nameof(UserTokens);

        // Application Tables
        internal const string RefreshTokens = nameof(RefreshTokens);
        internal const string Groups = nameof(Groups);
        internal const string GroupMembers = nameof(GroupMembers);
        internal const string Conversations = nameof(Conversations);
        internal const string ConversationParticipants = nameof(ConversationParticipants);
        internal const string Messages = nameof(Messages);
        internal const string MessageReactions = nameof(MessageReactions);
        internal const string MessageMentions = nameof(MessageMentions);
        internal const string MessageReadStatus = nameof(MessageReadStatus);
        internal const string Posts = nameof(Posts);
        internal const string PostComments = nameof(PostComments);
        internal const string PostLikes = nameof(PostLikes);
        internal const string Polls = nameof(Polls);
        internal const string PollOptions = nameof(PollOptions);
        internal const string PollVotes = nameof(PollVotes);
        internal const string VideoCallSessions = nameof(VideoCallSessions);
        internal const string VideoCallParticipants = nameof(VideoCallParticipants);
        internal const string SharedFiles = nameof(SharedFiles);
        internal const string GroupInvitations = nameof(GroupInvitations);
        internal const string Notifications = nameof(Notifications);
        internal const string NotificationObjectLinks = nameof(NotificationObjectLinks);
        internal const string UserGroupInvitations = nameof(UserGroupInvitations);
        internal const string PostAttachments = nameof(PostAttachments);
    }
}
