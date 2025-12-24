using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace FastBiteGroupMCA.Persistentce.Repositories.Efcore
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction; 

        // --- Các trường private để chứa các instance của repository (sẽ được khởi tạo lười) ---
        private IGroupsRepository? _groups;
        private IGroupMembersRepository? _groupMembers;
        private IConversationsRepository? _conversations;
        private IConversationParticipantsRepository? _conversationParticipants;
        private IPostsRepository? _posts;
        private IPostCommentsRepository? _postComments;
        private IPostLikesRepository? _postLikes;
        private IPollsRepository? _polls;
        private IPollOptionsRepository? _pollOptions;
        private IPollVotesRepository? _pollVotes;
        private IVideoCallSessionsRepository? _videoCallSessions;
        private IVideoCallParticipantsRepository? _videoCallParticipants;
        private ISharedFilesRepository? _sharedFiles;
        private IGroupInvitationsRepository? _groupInvitations;
        private IUserRepository? _user;
        private IGenericRepository<AppRole> _roles;
        private IGenericRepository<IdentityUserRole<Guid>> _userRoles;
        private IUserGroupInvitationRepository? _userGroupInvitation;
        private IPostAttachmentRepository? _postAttachments;
        private IRefreshTokenRepository? _refreshToken;
        private IContentReportsRepository? _contentReports;
        private IAdminNotificationsRepository? _adminNotifications;
        private IGlobalSettingsRepository? _globalSettings;
        private IAdminAuditLogRepository? _adminAuditLogs;

        /// <summary>
        /// Constructor giờ đây chỉ cần inject DbContext. Rất gọn gàng!
        /// </summary>
        public UnitOfWork(ApplicationDbContext context) => _context = context; // Use primary constructor to fix IDE0290

        public IGroupsRepository Groups => _groups ??= new GroupsRepository(_context);
        public IGroupMembersRepository GroupMembers => _groupMembers ??= new GroupMembersRepository(_context);
        public IConversationsRepository Conversations => _conversations ??= new ConversationsRepository(_context);
        public IConversationParticipantsRepository ConversationParticipants => _conversationParticipants ??= new ConversationParticipantsRepository(_context);
        public IPostsRepository Posts => _posts ??= new PostsRepository(_context);
        public IPostCommentsRepository PostComments => _postComments ??= new PostCommentsRepository(_context);
        public IPostLikesRepository PostLikes => _postLikes ??= new PostLikesRepository(_context);
        public IPollsRepository Polls => _polls ??= new PollsRepository(_context);
        public IPollOptionsRepository PollOptions => _pollOptions ??= new PollOptionsRepository(_context);
        public IPollVotesRepository PollVotes => _pollVotes ??= new PollVotesRepository(_context);
        public IVideoCallSessionsRepository VideoCallSessions => _videoCallSessions ??= new VideoCallSessionsRepository(_context);
        public IVideoCallParticipantsRepository VideoCallParticipants => _videoCallParticipants ??= new VideoCallParticipantsRepository(_context);
        public ISharedFilesRepository SharedFiles => _sharedFiles ??= new SharedFilesRepository(_context);
        public IGroupInvitationsRepository GroupInvitations => _groupInvitations ??= new GroupInvitationsRepository(_context);
        public IUserRepository Users => _user ??= new UserRepository(_context);
        public IGenericRepository<AppRole> Roles => _roles ??= new GenericRepository<AppRole>(_context);
        public IGenericRepository<IdentityUserRole<Guid>> UserRoles => _userRoles ??= new GenericRepository<IdentityUserRole<Guid>>(_context);
        public IUserGroupInvitationRepository UserGroupInvitations => _userGroupInvitation ??= new UserGroupInvitationRepository(_context);
        public IPostAttachmentRepository PostAttachments => _postAttachments ??= new PostAttachmentRepository(_context);
        public IRefreshTokenRepository RefreshToken => _refreshToken ??= new RefreshTokenRepository(_context);
        public IContentReportsRepository ContentReports => _contentReports ??= new ContentReportsRepository(_context);
        public IAdminNotificationsRepository AdminNotifications => _adminNotifications ??= new AdminNotificationsRepository(_context);
        public IGlobalSettingsRepository GlobalSettings => _globalSettings ??= new GlobalSettingsRepository(_context);
        public ILoginHistoryRepository LoginHistories => new LoginHistoryRepository(_context);
        public IAdminAuditLogRepository AdminAuditLogs => _adminAuditLogs ??= new AdminAuditLogRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null!;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null!;
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
        public DbContext GetDbContext()
        {
            return _context;
        }
    }
}
