using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FastBiteGroupMCA.Persistentce.Migrations
{
    /// <inheritdoc />
    public partial class Inittier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.SettingKey);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FisrtName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PresenceStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MessagingPrivacy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OneSignalPlayerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminFullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    TargetEntityType = table.Column<int>(type: "int", nullable: false),
                    TargetEntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminNotifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LinkTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminNotifications_Users_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LoginHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedFiles",
                columns: table => new
                {
                    FileID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StorageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedByUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    FileContext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedFiles", x => x.FileID);
                    table.ForeignKey(
                        name: "FK_SharedFiles_Users_UploadedByUserID",
                        column: x => x.UploadedByUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentReports",
                columns: table => new
                {
                    ContentReportID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportedContentID = table.Column<int>(type: "int", nullable: false),
                    ReportedContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportedByUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedContentOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReports", x => x.ContentReportID);
                    table.ForeignKey(
                        name: "FK_ContentReports_Users_ReportedByUserID",
                        column: x => x.ReportedByUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipants",
                columns: table => new
                {
                    ConversationParticipantID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReadTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsMuted = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipants", x => x.ConversationParticipantID);
                    table.ForeignKey(
                        name: "FK_ConversationParticipants_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    ConversationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ExplicitGroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastMessagePreview = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastMessageTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMessageSenderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.ConversationID);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GroupType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Privacy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroupAvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ConversationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupID);
                    table.ForeignKey(
                        name: "FK_Groups_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "ConversationID");
                    table.ForeignKey(
                        name: "FK_Groups_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    PollID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationID = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AllowMultipleChoices = table.Column<bool>(type: "bit", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MessageID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => x.PollID);
                    table.ForeignKey(
                        name: "FK_Polls_Conversations_ConversationID",
                        column: x => x.ConversationID,
                        principalTable: "Conversations",
                        principalColumn: "ConversationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Polls_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VideoCallSessions",
                columns: table => new
                {
                    VideoCallSessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConversationID = table.Column<int>(type: "int", nullable: false),
                    InitiatorUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeoutJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoCallSessions", x => x.VideoCallSessionID);
                    table.ForeignKey(
                        name: "FK_VideoCallSessions_Conversations_ConversationID",
                        column: x => x.ConversationID,
                        principalTable: "Conversations",
                        principalColumn: "ConversationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoCallSessions_Users_InitiatorUserID",
                        column: x => x.InitiatorUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupInvitations",
                columns: table => new
                {
                    InvitationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxUses = table.Column<int>(type: "int", nullable: true),
                    CurrentUses = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupInvitations", x => x.InvitationID);
                    table.ForeignKey(
                        name: "FK_GroupInvitations_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupInvitations_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    GroupMemberID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.GroupMemberID);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    PostID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.PostID);
                    table.ForeignKey(
                        name: "FK_Posts_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Posts_Users_AuthorUserID",
                        column: x => x.AuthorUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupInvitations",
                columns: table => new
                {
                    InvitationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedByUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupInvitations", x => x.InvitationID);
                    table.ForeignKey(
                        name: "FK_UserGroupInvitations_Groups_GroupID",
                        column: x => x.GroupID,
                        principalTable: "Groups",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroupInvitations_Users_InvitedByUserID",
                        column: x => x.InvitedByUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserGroupInvitations_Users_InvitedUserID",
                        column: x => x.InvitedUserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PollOptions",
                columns: table => new
                {
                    PollOptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollID = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollOptions", x => x.PollOptionID);
                    table.ForeignKey(
                        name: "FK_PollOptions_Polls_PollID",
                        column: x => x.PollID,
                        principalTable: "Polls",
                        principalColumn: "PollID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoCallParticipants",
                columns: table => new
                {
                    VideoCallParticipantID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoCallSessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoCallParticipants", x => x.VideoCallParticipantID);
                    table.ForeignKey(
                        name: "FK_VideoCallParticipants_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoCallParticipants_VideoCallSessions_VideoCallSessionID",
                        column: x => x.VideoCallSessionID,
                        principalTable: "VideoCallSessions",
                        principalColumn: "VideoCallSessionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostAttachments",
                columns: table => new
                {
                    PostID = table.Column<int>(type: "int", nullable: false),
                    FileID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostAttachments", x => new { x.PostID, x.FileID });
                    table.ForeignKey(
                        name: "FK_PostAttachments_Posts_PostID",
                        column: x => x.PostID,
                        principalTable: "Posts",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostAttachments_SharedFiles_FileID",
                        column: x => x.FileID,
                        principalTable: "SharedFiles",
                        principalColumn: "FileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostComments",
                columns: table => new
                {
                    CommentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ParentCommentID = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostComments", x => x.CommentID);
                    table.ForeignKey(
                        name: "FK_PostComments_PostComments_ParentCommentID",
                        column: x => x.ParentCommentID,
                        principalTable: "PostComments",
                        principalColumn: "CommentID");
                    table.ForeignKey(
                        name: "FK_PostComments_Posts_PostID",
                        column: x => x.PostID,
                        principalTable: "Posts",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostComments_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostLikes",
                columns: table => new
                {
                    LikeID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostLikes", x => x.LikeID);
                    table.ForeignKey(
                        name: "FK_PostLikes_Posts_PostID",
                        column: x => x.PostID,
                        principalTable: "Posts",
                        principalColumn: "PostID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostLikes_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollVotes",
                columns: table => new
                {
                    PollVoteID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollOptionID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVotes", x => x.PollVoteID);
                    table.ForeignKey(
                        name: "FK_PollVotes_PollOptions_PollOptionID",
                        column: x => x.PollOptionID,
                        principalTable: "PollOptions",
                        principalColumn: "PollOptionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PollVotes_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "GlobalSettings",
                columns: new[] { "SettingKey", "SettingValue" },
                values: new object[,]
                {
                    { "AllowedAvatarTypes", "jpg,jpeg,png,gif" },
                    { "AllowedFileTypes", "jpg,jpeg,png,gif,pdf,docx,xlsx,zip" },
                    { "AllowNewRegistrations", "true" },
                    { "AutoLockAccountThreshold", "0" },
                    { "DefaultRoleForNewUsers", "Customer" },
                    { "ForbiddenKeywords", "" },
                    { "MaintenanceMode", "false" },
                    { "MaxAvatarSizeMb", "2" },
                    { "MaxFileSizeMb", "10" },
                    { "RequireEmailConfirmation", "true" },
                    { "SiteName", "FastBite MCA" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "IsSystemRole", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-1234-567890abcdef"), "9f791064-36bd-4af6-8192-7769c123193b", true, "Admin", "ADMIN" },
                    { new Guid("b2c3d4e5-f6a7-8901-2345-67890abcdef1"), "9f791064-36bd-4af6-8192-7769c123193b", true, "Customer", "CUSTOMER" },
                    { new Guid("e2a9bfc7-d3f4-4a6b-9c8d-10ffbca9876e"), "9f791064-36bd-4af6-8192-7769c123193b", true, "VIP", "VIP" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AccessFailedCount", "AvatarUrl", "Bio", "ConcurrencyStamp", "CreatedAt", "DateOfBirth", "Email", "EmailConfirmed", "FisrtName", "FullName", "IsActive", "IsDeleted", "LastName", "LockoutEnabled", "LockoutEnd", "MessagingPrivacy", "NormalizedEmail", "NormalizedUserName", "OneSignalPlayerId", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "PresenceStatus", "SecurityStamp", "TwoFactorEnabled", "UpdatedAt", "UserName" },
                values: new object[,]
                {
                    { new Guid("c0e8fad7-d01b-483c-bf0d-08ddc9b9460d"), 0, null, null, "0fd7ab16-c716-4985-8420-b9e0e8821dd8", new DateTime(2025, 9, 21, 16, 53, 32, 84, DateTimeKind.Utc).AddTicks(832), new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "superadmin@app.com", true, "Super", "Super Admin", true, false, "Admin", false, null, "FromSharedGroupMembers", "SUPERADMIN@APP.COM", "SUPERADMIN@APP.COM", null, "AQAAAAIAAYagAAAAEMh7x7zHRm5zXjhhH4t4+Bp2HgzmLb9pnIFl9MaGvHyKFTui5l9WpyCCMcggOC0dNQ==", null, false, "Offline", "fd7d18eb-2d34-4249-8033-f1b2397d85a1", false, null, "superadmin@app.com" },
                    { new Guid("d1f9aeb8-e2c3-4b5a-8d6f-09eecba8765e"), 0, null, null, "c08da64f-9c0a-4951-b675-f56f6f48bb70", new DateTime(2025, 9, 21, 16, 53, 32, 122, DateTimeKind.Utc).AddTicks(6823), new DateTime(2000, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "testuser@app.com", true, "Test", "Test User", true, false, "User", false, null, "FromSharedGroupMembers", "TESTUSER@APP.COM", "TESTUSER@APP.COM", null, "AQAAAAIAAYagAAAAEOZloerFRtzAqd0c91XhgjLmFVHn81jQ6UMI7jwUjTff3ye+Vm/e9Gdpg63bcwfWJw==", null, false, "Offline", "48b01ae4-c117-4ff2-8314-b5a9750fc4da", false, null, "testuser@app.com" }
                });

            migrationBuilder.InsertData(
                table: "Groups",
                columns: new[] { "GroupID", "ConversationId", "CreatedAt", "CreatedByUserID", "DeletedAt", "Description", "GroupAvatarUrl", "GroupName", "GroupType", "IsArchived", "IsDeleted", "Privacy", "UpdatedAt" },
                values: new object[] { new Guid("e2a8b9c7-d3f4-4a6b-9c8d-10ffbca9876f"), null, new DateTime(2025, 9, 21, 16, 53, 32, 122, DateTimeKind.Utc).AddTicks(7317), new Guid("c0e8fad7-d01b-483c-bf0d-08ddc9b9460d"), null, "Đây là một nhóm được tạo sẵn để thảo luận.", "https://your-cdn.com/default-group-avatar.png", "Nhóm Chat Công khai", "Public", false, false, "Public", null });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-1234-567890abcdef"), new Guid("c0e8fad7-d01b-483c-bf0d-08ddc9b9460d") },
                    { new Guid("b2c3d4e5-f6a7-8901-2345-67890abcdef1"), new Guid("c0e8fad7-d01b-483c-bf0d-08ddc9b9460d") },
                    { new Guid("b2c3d4e5-f6a7-8901-2345-67890abcdef1"), new Guid("d1f9aeb8-e2c3-4b5a-8d6f-09eecba8765e") }
                });

            migrationBuilder.InsertData(
                table: "Conversations",
                columns: new[] { "ConversationID", "AvatarUrl", "ConversationType", "CreatedAt", "ExplicitGroupID", "IsDeleted", "LastMessagePreview", "LastMessageSenderName", "LastMessageTimestamp", "Title", "UpdatedAt" },
                values: new object[] { 1, "https://your-cdn.com/default-group-avatar.png", "Group", new DateTime(2025, 9, 21, 16, 53, 32, 122, DateTimeKind.Utc).AddTicks(7366), new Guid("e2a8b9c7-d3f4-4a6b-9c8d-10ffbca9876f"), false, null, null, null, "Nhóm Chat Công khai", null });

            migrationBuilder.InsertData(
                table: "GroupMembers",
                columns: new[] { "GroupMemberID", "GroupID", "JoinedAt", "Role", "UserID" },
                values: new object[] { 2, new Guid("e2a8b9c7-d3f4-4a6b-9c8d-10ffbca9876f"), new DateTime(2025, 9, 21, 16, 53, 32, 122, DateTimeKind.Utc).AddTicks(7432), "Member", new Guid("d1f9aeb8-e2c3-4b5a-8d6f-09eecba8765e") });

            migrationBuilder.InsertData(
                table: "Posts",
                columns: new[] { "PostID", "AuthorUserID", "ContentHtml", "ContentJson", "CreatedAt", "GroupID", "IsDeleted", "IsPinned", "Status", "Title", "UpdatedAt" },
                values: new object[] { 1, new Guid("c0e8fad7-d01b-483c-bf0d-08ddc9b9460d"), "<p>Đây là bài viết đầu tiên trong nhóm.</p>", "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Đây là bài viết đầu tiên trong nhóm.\"}]}]}", new DateTime(2025, 9, 21, 16, 53, 32, 122, DateTimeKind.Utc).AddTicks(7511), new Guid("e2a8b9c7-d3f4-4a6b-9c8d-10ffbca9876f"), false, true, 1, "Chào mừng đến với ứng dụng!", null });

            migrationBuilder.InsertData(
                table: "ConversationParticipants",
                columns: new[] { "ConversationParticipantID", "ConversationID", "IsArchived", "IsMuted", "JoinedAt", "LastReadTimestamp", "UserID" },
                values: new object[] { 2, 1, false, false, new DateTime(2025, 9, 21, 16, 53, 32, 122, DateTimeKind.Utc).AddTicks(7462), null, new Guid("d1f9aeb8-e2c3-4b5a-8d6f-09eecba8765e") });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUserId",
                table: "AdminAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_TriggeredByUserId",
                table: "AdminNotifications",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_GroupID",
                table: "ContentReports",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReports_ReportedByUserID",
                table: "ContentReports",
                column: "ReportedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_ConversationID_UserID",
                table: "ConversationParticipants",
                columns: new[] { "ConversationID", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_UserID",
                table: "ConversationParticipants",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ExplicitGroupID",
                table: "Conversations",
                column: "ExplicitGroupID",
                unique: true,
                filter: "[ExplicitGroupID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupInvitations_CreatedByUserID",
                table: "GroupInvitations",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupInvitations_GroupID",
                table: "GroupInvitations",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_GroupInvitations_InvitationCode",
                table: "GroupInvitations",
                column: "InvitationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupID_UserID",
                table: "GroupMembers",
                columns: new[] { "GroupID", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserID",
                table: "GroupMembers",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ConversationId",
                table: "Groups",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CreatedByUserID",
                table: "Groups",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_UserId",
                table: "LoginHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PollOptions_PollID",
                table: "PollOptions",
                column: "PollID");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_ConversationID",
                table: "Polls",
                column: "ConversationID");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_CreatedByUserID",
                table: "Polls",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_PollOptionID_UserID",
                table: "PollVotes",
                columns: new[] { "PollOptionID", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_UserID",
                table: "PollVotes",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PostAttachments_FileID",
                table: "PostAttachments",
                column: "FileID");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_ParentCommentID",
                table: "PostComments",
                column: "ParentCommentID");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_PostID",
                table: "PostComments",
                column: "PostID");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_UserID",
                table: "PostComments",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_PostID_UserID",
                table: "PostLikes",
                columns: new[] { "PostID", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_UserID",
                table: "PostLikes",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorUserID",
                table: "Posts",
                column: "AuthorUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_GroupID_CreatedAt",
                table: "Posts",
                columns: new[] { "GroupID", "CreatedAt" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AppUserId",
                table: "RefreshTokens",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SharedFiles_UploadedByUserID",
                table: "SharedFiles",
                column: "UploadedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Unique_User_Group_Invitation",
                table: "UserGroupInvitations",
                columns: new[] { "InvitedUserID", "GroupID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupInvitations_GroupID",
                table: "UserGroupInvitations",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupInvitations_InvitedByUserID",
                table: "UserGroupInvitations",
                column: "InvitedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VideoCallParticipants_UserID",
                table: "VideoCallParticipants",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_VideoCallParticipants_VideoCallSessionID_UserID",
                table: "VideoCallParticipants",
                columns: new[] { "VideoCallSessionID", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoCallSessions_ConversationID",
                table: "VideoCallSessions",
                column: "ConversationID");

            migrationBuilder.CreateIndex(
                name: "IX_VideoCallSessions_InitiatorUserID",
                table: "VideoCallSessions",
                column: "InitiatorUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentReports_Groups_GroupID",
                table: "ContentReports",
                column: "GroupID",
                principalTable: "Groups",
                principalColumn: "GroupID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationParticipants_Conversations_ConversationID",
                table: "ConversationParticipants",
                column: "ConversationID",
                principalTable: "Conversations",
                principalColumn: "ConversationID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Groups_ExplicitGroupID",
                table: "Conversations",
                column: "ExplicitGroupID",
                principalTable: "Groups",
                principalColumn: "GroupID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_CreatedByUserID",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Groups_ExplicitGroupID",
                table: "Conversations");

            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "AdminNotifications");

            migrationBuilder.DropTable(
                name: "ContentReports");

            migrationBuilder.DropTable(
                name: "ConversationParticipants");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "GroupInvitations");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "LoginHistories");

            migrationBuilder.DropTable(
                name: "PollVotes");

            migrationBuilder.DropTable(
                name: "PostAttachments");

            migrationBuilder.DropTable(
                name: "PostComments");

            migrationBuilder.DropTable(
                name: "PostLikes");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserGroupInvitations");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "VideoCallParticipants");

            migrationBuilder.DropTable(
                name: "PollOptions");

            migrationBuilder.DropTable(
                name: "SharedFiles");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "VideoCallSessions");

            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Conversations");
        }
    }
}
