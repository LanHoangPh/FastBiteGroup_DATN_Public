using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;

public interface IUnitOfWork : IDisposable
{
    IGroupsRepository Groups { get; }
    IGroupMembersRepository GroupMembers { get; }
    IConversationsRepository Conversations { get; }
    IConversationParticipantsRepository ConversationParticipants { get; }
    IPostsRepository Posts { get; }
    IPostCommentsRepository PostComments { get; }
    IPostLikesRepository PostLikes { get; }
    IPollsRepository Polls { get; }
    IPollOptionsRepository PollOptions { get; }
    IPollVotesRepository PollVotes { get; }
    IVideoCallSessionsRepository VideoCallSessions { get; }
    IVideoCallParticipantsRepository VideoCallParticipants { get; }
    ISharedFilesRepository SharedFiles { get; }
    IGroupInvitationsRepository GroupInvitations { get; }
    IUserRepository Users { get; }
    IGenericRepository<AppRole> Roles { get; }
    IGenericRepository<IdentityUserRole<Guid>> UserRoles { get; }
    IUserGroupInvitationRepository UserGroupInvitations { get; }
    IPostAttachmentRepository PostAttachments { get; }
    IRefreshTokenRepository RefreshToken { get; }
    IContentReportsRepository ContentReports { get; }
    IAdminNotificationsRepository AdminNotifications { get; }
    IGlobalSettingsRepository GlobalSettings { get; }
    ILoginHistoryRepository LoginHistories { get; }
    IAdminAuditLogRepository AdminAuditLogs { get; }
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task<int> SaveChangesAsync();
    DbContext GetDbContext();
}
