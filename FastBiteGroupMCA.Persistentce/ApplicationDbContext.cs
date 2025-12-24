using FastBiteGroupMCA.Domain.Abstractions.Enities;
using FastBiteGroupMCA.Domain.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FastBiteGroupMCA.Persistentce;

public sealed class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<PostAttachment> PostAttachments { get; set; }
    public DbSet<ConversationParticipants> ConversationParticipants { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupInvitations> GroupInvitations { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserGroupInvitation> UserGroupInvitations { get; set; }
    public DbSet<Polls> Polls { get; set; }
    public DbSet<PollOptions> PollOptions { get; set; }
    public DbSet<PollVotes> PollVotes { get; set; }
    public DbSet<PostLikes> PostLikes { get; set; }
    public DbSet<Posts> Posts { get; set; }
    public DbSet<PostComments> PostComments { get; set; }
    public DbSet<SharedFiles> SharedFiles { get; set; }
    public DbSet<VideoCallSessions> VideoCallSessions { get; set; }
    public DbSet<VideoCallParticipants> VideoCallParticipants { get; set; }
    public DbSet<ContentReport> ContentReports { get; set; }
    public DbSet<AdminNotifications> AdminNotifications { get; set; }
    public DbSet<GlobalSettings> GlobalSettings { get; set; }

    public DbSet<LoginHistory> LoginHistories { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(AssemblyReference.Assenmbly);
        //SeedRoles(builder);
        SeedPost(builder);
        SeedData2(builder);

        //Đặt filter tương ứng cho TẤT CẢ các entity con ---

        // Con của AppUser
        builder.Entity<GroupInvitations>().HasQueryFilter(e => !e.CreatedByUser!.IsDeleted);
        builder.Entity<PollVotes>().HasQueryFilter(e => !e.User!.IsDeleted);
        builder.Entity<RefreshToken>().HasQueryFilter(e => !e.User!.IsDeleted);
        builder.Entity<VideoCallParticipants>().HasQueryFilter(e => !e.User!.IsDeleted);
        builder.Entity<LoginHistory>().HasQueryFilter(e => !e.User!.IsDeleted);
        // Con của Group
        builder.Entity<GroupMember>().HasQueryFilter(e => !e.Group!.IsDeleted);
        // GroupInvitations cũng là con của Group
        builder.Entity<GroupInvitations>().HasQueryFilter(e => !e.Group!.IsDeleted && !e.CreatedByUser!.IsDeleted); // Kết hợp nếu cần
        builder.Entity<UserGroupInvitation>().HasQueryFilter(e => !e.Group!.IsDeleted && !e.InvitedUser!.IsDeleted && !e.InvitedByUser!.IsDeleted); // Kết hợp nếu cần
                                                                                                                                                    // Posts cũng là con của Group
        builder.Entity<Posts>().HasQueryFilter(e => !e.Group!.IsDeleted && !e.Author!.IsDeleted); // Kết hợp

        // Con của Conversation
        builder.Entity<ConversationParticipants>().HasQueryFilter(e => !e.Conversation!.IsDeleted);

        // Con của Polls
        builder.Entity<PollOptions>().HasQueryFilter(e => !e.Poll!.IsDeleted);
        // PollVotes cũng là con của Polls (qua PollOptions)
        builder.Entity<PollVotes>().HasQueryFilter(e => !e.PollOption!.Poll!.IsDeleted && !e.User!.IsDeleted);

        // Con của Posts
        builder.Entity<PostLikes>().HasQueryFilter(e => !e.Post!.IsDeleted);
        builder.Entity<PostComments>().HasQueryFilter(e => !e.Post!.IsDeleted);
        builder.Entity<PostAttachment>().HasQueryFilter(e => !e.Post!.IsDeleted);
        builder.Entity<ContentReport>().HasQueryFilter(e =>!e.ReportedByUser!.IsDeleted);
    }

    private void SeedData2(ModelBuilder builder)
    {
        // --- BƯỚC 1: TẠO CÁC ID CỐ ĐỊNH ĐỂ DỄ DÀNG LIÊN KẾT ---
        var adminRoleId = new Guid("a1b2c3d4-e5f6-7890-1234-567890abcdef");
        var customerRoleId = new Guid("b2c3d4e5-f6a7-8901-2345-67890abcdef1");

        var adminUserId = new Guid("c0e8fad7-d01b-483c-bf0d-08ddc9b9460d");
        var testUserId = new Guid("d1f9aeb8-e2c3-4b5a-8d6f-09eecba8765e");
        var anotherUserId = new Guid("e2a9bfc7-d3f4-4a6b-9c8d-10ffbca9876e");
        var concurrencyStamp = Guid.NewGuid().ToString();

        var publicGroupId = new Guid("e2a8b9c7-d3f4-4a6b-9c8d-10ffbca9876f");

        // --- BƯỚC 2: SEED ROLES (VAI TRÒ) ---
        builder.Entity<AppRole>().HasData(
            new AppRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = concurrencyStamp, IsSystemRole = true },
            new AppRole { Id = customerRoleId, Name = "Customer", NormalizedName = "CUSTOMER", ConcurrencyStamp = concurrencyStamp, IsSystemRole = true },
            new AppRole { Id = anotherUserId, Name = "VIP", NormalizedName = "VIP", ConcurrencyStamp = concurrencyStamp ,IsSystemRole = true }
        );

        // --- BƯỚC 3: SEED USERS (NGƯỜI DÙNG) ---
        var passwordHasher = new PasswordHasher<AppUser>();
        var adminUser = new AppUser
        {
            Id = adminUserId,
            UserName = "superadmin@app.com",
            NormalizedUserName = "SUPERADMIN@APP.COM",
            Email = "superadmin@app.com",
            NormalizedEmail = "SUPERADMIN@APP.COM",
            FisrtName = "Super", // Sửa lại lỗi chính tả FisrtName -> FirstName trong entity của bạn sẽ tốt hơn
            LastName = "Admin",
            FullName = "Super Admin",
            DateOfBirth = new DateTime(1990, 1, 1),
            EmailConfirmed = true,
            PasswordHash = passwordHasher.HashPassword(null, "Admin@123"),
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var testUser = new AppUser
        {
            Id = testUserId,
            UserName = "testuser@app.com",
            NormalizedUserName = "TESTUSER@APP.COM",
            Email = "testuser@app.com",
            NormalizedEmail = "TESTUSER@APP.COM",
            FisrtName = "Test",
            LastName = "User",
            FullName = "Test User",
            DateOfBirth = new DateTime(2000, 5, 10),
            EmailConfirmed = true,
            PasswordHash = passwordHasher.HashPassword(null, "User@123"),
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };
        builder.Entity<AppUser>().HasData(adminUser, testUser);

        // --- BƯỚC 4: SEED USER-ROLE RELATIONSHIPS (GÁN VAI TRÒ) ---
        builder.Entity<IdentityUserRole<Guid>>().HasData(
            new IdentityUserRole<Guid> { RoleId = adminRoleId, UserId = adminUserId },
            new IdentityUserRole<Guid> { RoleId = customerRoleId, UserId = testUserId },
            new IdentityUserRole<Guid> { RoleId = customerRoleId, UserId = adminUserId } // Admin cũng là Customer
        );

        // --- BƯỚC 5: SEED GROUP, CONVERSATION, VÀ CÁC LIÊN KẾT ---
        var group = new Group
        {
            GroupID = publicGroupId,
            GroupName = "Nhóm Chat Công khai",
            Description = "Đây là một nhóm được tạo sẵn để thảo luận.",
            GroupType = EnumGroupType.Public,
            Privacy = EnumGroupPrivacy.Public,
            GroupAvatarUrl = "https://your-cdn.com/default-group-avatar.png",
            CreatedByUserID = adminUserId,
            CreatedAt = DateTime.UtcNow
        };
        builder.Entity<Group>().HasData(group);

        var conversation = new Conversation
        {
            ConversationID = 1, // Dùng ID cố định cho dễ
            ConversationType = EnumConversationType.Group,
            Title = "Nhóm Chat Công khai",
            AvatarUrl = "https://your-cdn.com/default-group-avatar.png",
            ExplicitGroupID = publicGroupId,
            CreatedAt = DateTime.UtcNow
        };
        builder.Entity<Conversation>().HasData(conversation);

        builder.Entity<GroupMember>().HasData(
            new GroupMember { GroupMemberID = 2, GroupID = publicGroupId, UserID = testUserId, Role = EnumGroupRole.Member, JoinedAt = DateTime.UtcNow }
        );

        builder.Entity<ConversationParticipants>().HasData(
            new ConversationParticipants { ConversationParticipantID = 2, ConversationID = 1, UserID = testUserId, JoinedAt = DateTime.UtcNow }
        );

        // --- BƯỚC 6: SEED POSTS (VỚI DỮ LIỆU ĐẦY ĐỦ) ---
        builder.Entity<Posts>().HasData(
            new Posts
            {
                PostID = 1,
                GroupID = publicGroupId,
                AuthorUserID = adminUserId,
                Title = "Chào mừng đến với ứng dụng!",
                ContentJson = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Đây là bài viết đầu tiên trong nhóm.\"}]}]}",
                ContentHtml = "<p>Đây là bài viết đầu tiên trong nhóm.</p>", // HTML đã render
                Status = EnumPostStatus.Published, // Trạng thái đã duyệt
                IsPinned = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
    private static void SeedPost(ModelBuilder builder)
    {
        // === 4. SEED GLOBAL SETTINGS ===
        builder.Entity<GlobalSettings>().HasData(
            // --- General Settings ---
            new GlobalSettings { SettingKey = SettingKeys.SiteName.ToString(), SettingValue = "FastBite MCA" },
            new GlobalSettings { SettingKey = SettingKeys.MaintenanceMode.ToString(), SettingValue = "false" },

            // --- User Management ---
            new GlobalSettings { SettingKey = SettingKeys.AllowNewRegistrations.ToString(), SettingValue = "true" },
            new GlobalSettings { SettingKey = SettingKeys.RequireEmailConfirmation.ToString(), SettingValue = "true" },
            new GlobalSettings { SettingKey = SettingKeys.DefaultRoleForNewUsers.ToString(), SettingValue = "Customer" },

            // --- Content & Moderation ---
            new GlobalSettings { SettingKey = SettingKeys.ForbiddenKeywords.ToString(), SettingValue = "" }, // Mặc định là chuỗi rỗng
            new GlobalSettings { SettingKey = SettingKeys.AutoLockAccountThreshold.ToString(), SettingValue = "0" }, // Mặc định là 0 (tắt tính năng)

            // --- File Uploads ---
            new GlobalSettings { SettingKey = SettingKeys.MaxFileSizeMb.ToString(), SettingValue = "10" },
            new GlobalSettings { SettingKey = SettingKeys.MaxAvatarSizeMb.ToString(), SettingValue = "2" },
            new GlobalSettings { SettingKey = SettingKeys.AllowedFileTypes.ToString(), SettingValue = "jpg,jpeg,png,gif,pdf,docx,xlsx,zip" },
            new GlobalSettings { SettingKey = SettingKeys.AllowedAvatarTypes.ToString(), SettingValue = "jpg,jpeg,png,gif" }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    private void OnBeforeSaving()
    {
        var entries = ChangeTracker.Entries<IDateTracking>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
