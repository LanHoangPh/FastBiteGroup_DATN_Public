using Bogus;
using FastBiteGroupMCA.Domain.Enum;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using System.Text.Json;

namespace FastBiteGroupMCA.Persistentce.SeedData;

public static class DataGenerator
{
    private const int NUMBER_OF_USERS = 20;
    private const int NUMBER_OF_GROUPS = 10;
    private const int POSTS_PER_GROUP = 20;
    private const int COMMENTS_PER_POST = 15;
    private const int MESSAGES_PER_CONVERSATION = 50;
    private const int NUMBER_OF_REPORTS = 5;

    public static GeneratedData Seed()
    {
        Randomizer.Seed = new Random(8675309);
        var faker = new Faker("vi");

        var userFaker = new Faker<AppUser>("vi")
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.FisrtName, f => f.Name.FirstName()) // Nên sửa lại lỗi chính tả "FisrtName" trong AppUser.cs
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.FullName, (f, u) => $"{u.FisrtName} {u.LastName}")
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FisrtName, u.LastName).ToLower())
            .RuleFor(u => u.NormalizedEmail, (f, u) => u.Email.ToUpper())
            .RuleFor(u => u.UserName, (f, u) => u.Email)
            .RuleFor(u => u.NormalizedUserName, (f, u) => u.Email.ToUpper())
            .RuleFor(u => u.AvatarUrl, f => f.Internet.Avatar())
            .RuleFor(u => u.Bio, f => f.Lorem.Sentence(10))
            .RuleFor(u => u.DateOfBirth, f => f.Date.Past(30, DateTime.Now.AddYears(-18)))
            .RuleFor(u => u.IsActive, true)
            .RuleFor(u => u.EmailConfirmed, true)
            .RuleFor(u => u.PresenceStatus, f => f.PickRandom<EnumUserPresenceStatus>())
            .RuleFor(u => u.MessagingPrivacy, f => f.PickRandom<EnumMessagingPrivacy>()) // Bổ sung
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2))
            .RuleFor(u => u.SecurityStamp, f => Guid.NewGuid().ToString());

        var users = userFaker.Generate(NUMBER_OF_USERS);
        var passwordHasher = new PasswordHasher<AppUser>();
        users.ForEach(u => u.PasswordHash = passwordHasher.HashPassword(u, "User@123"));

        // 2. Tạo Groups
        var groupFaker = new Faker<Group>("vi")
            .RuleFor(g => g.GroupID, f => Guid.NewGuid())
            .RuleFor(g => g.GroupName, f => f.Commerce.ProductName() + " " + f.PickRandom("Club", "Team", "Community", "Hub"))
            .RuleFor(g => g.Description, f => f.Lorem.Paragraph(2))
            .RuleFor(g => g.GroupType, f => f.PickRandom(new[] { EnumGroupType.Public, EnumGroupType.Community }))
            .RuleFor(g => g.Privacy, (f, g) => g.GroupType == EnumGroupType.Community ? EnumGroupPrivacy.Public : f.PickRandom<EnumGroupPrivacy>()) // Bổ sung
            .RuleFor(g => g.GroupAvatarUrl, f => f.Image.PicsumUrl())
            .RuleFor(g => g.CreatedByUserID, f => f.PickRandom(users).Id)
            .RuleFor(g => g.CreatedAt, f => f.Date.Past(1))
            .RuleFor(g => g.IsArchived, false); // Bổ sung

        var groups = groupFaker.Generate(NUMBER_OF_GROUPS);

        // --- 3. TẠO CÁC MỐI QUAN HỆ (MEMBERS, CONVERSATIONS) ---
        var groupMembers = new List<GroupMember>();
        var conversations = new List<Conversation>();
        var conversationParticipants = new List<ConversationParticipants>();
        int conversationIdCounter = 1;

        foreach(var group in groups)
        {
            // Người tạo nhóm luôn là Admin
            groupMembers.Add(new GroupMember { GroupMemberID = groupMembers.Count + 1, GroupID = group.GroupID, UserID = group.CreatedByUserID, Role = EnumGroupRole.Admin, JoinedAt = group.CreatedAt });

            // Nếu là nhóm chat, tạo Conversation và thêm người tạo vào
            if (group.GroupType != EnumGroupType.Community)
            {
                var conv = new Conversation
                {
                    ConversationID = conversationIdCounter,
                    ConversationType = EnumConversationType.Group,
                    ExplicitGroupID = group.GroupID,
                    Title = group.GroupName,
                    AvatarUrl = group.GroupAvatarUrl,
                    CreatedAt = group.CreatedAt
                };
                conversations.Add(conv);
                conversationParticipants.Add(new ConversationParticipants { ConversationParticipantID = conversationParticipants.Count + 1, ConversationID = conv.ConversationID, UserID = group.CreatedByUserID, JoinedAt = group.CreatedAt });
                conversationIdCounter++;
            }

            // Thêm các thành viên khác vào nhóm
            var membersToJoin = faker.PickRandom(users.Where(u => u.Id != group.CreatedByUserID), faker.Random.Int(2, 5)).ToList();
            foreach (var user in membersToJoin)
            {
                if (!groupMembers.Any(gm => gm.GroupID == group.GroupID && gm.UserID == user.Id))
                {
                    var member = new GroupMember { GroupMemberID = groupMembers.Count + 1, GroupID = group.GroupID, UserID = user.Id, Role = EnumGroupRole.Member, JoinedAt = faker.Date.Between(group.CreatedAt, DateTime.UtcNow) };
                    groupMembers.Add(member);

                    var conv = conversations.FirstOrDefault(c => c.ExplicitGroupID == group.GroupID);
                    if (conv != null)
                    {
                        conversationParticipants.Add(new ConversationParticipants { ConversationParticipantID = conversationParticipants.Count + 1, ConversationID = conv.ConversationID, UserID = user.Id, JoinedAt = member.JoinedAt });
                    }
                }
            }
        }

        // --- 4. TẠO POSTS (chỉ cho nhóm Community) ---
        var posts = new List<Posts>();
        var communityGroups = groups.Where(g => g.GroupType == EnumGroupType.Community).ToList();
        foreach (var group in communityGroups)
        {
            var memberIdsInGroup = groupMembers.Where(gm => gm.GroupID == group.GroupID).Select(gm => gm.UserID).ToList();
            if (!memberIdsInGroup.Any()) continue;

            var postFaker = new Faker<Posts>("vi")
                .RuleFor(p => p.PostID, f => posts.Count + 1)
                .RuleFor(p => p.GroupID, group.GroupID)
                .RuleFor(p => p.AuthorUserID, f => f.PickRandom(memberIdsInGroup))
                .RuleFor(p => p.Title, f => f.Lorem.Sentence(5))
                .RuleFor(p => p.ContentJson, f => "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"" + f.Lorem.Paragraph() + "\"}]}]}") // Bổ sung JSON
                .RuleFor(p => p.ContentHtml, (f, p) => $"<p>{JsonDocument.Parse(p.ContentJson).RootElement.GetProperty("content")[0].GetProperty("content")[0].GetProperty("text").GetString()}</p>") // Bổ sung HTML
                .RuleFor(p => p.Status, EnumPostStatus.Published) // Bổ sung
                .RuleFor(p => p.IsPinned, f => f.Random.Bool(0.1f))
                .RuleFor(p => p.CreatedAt, f => f.Date.Between(group.CreatedAt, DateTime.UtcNow));

            posts.AddRange(postFaker.Generate(POSTS_PER_GROUP));
        }

        // --- 5. TẠO POST COMMENTS (đã cải tiến) ---
        var comments = new List<PostComments>();
        int commentIdCounter = 1;

        foreach (var post in posts)
        {
            // Lấy danh sách thành viên của nhóm chứa bài viết này
            var memberIdsInGroup = groupMembers.Where(gm => gm.GroupID == post.GroupID).Select(gm => gm.UserID).ToList();
            if (!memberIdsInGroup.Any()) continue;

            // Tạo các bình luận cấp 1 (top-level)
            var topLevelComments = new List<PostComments>();
            var commentFaker = new Faker<PostComments>("vi")
                .RuleFor(c => c.CommentID, f => commentIdCounter++)
                .RuleFor(c => c.PostID, post.PostID)
                .RuleFor(c => c.UserID, f => f.PickRandom(memberIdsInGroup)) // Chỉ thành viên mới được bình luận
                .RuleFor(c => c.Content, f => f.Lorem.Sentences(f.Random.Int(1, 3)))
                .RuleFor(c => c.CreatedAt, f => f.Date.Between(post.CreatedAt, DateTime.UtcNow))
                .RuleFor(c => c.UpdatedAt, (f, c) => c.CreatedAt); // Giả định lúc tạo chưa có update

            var generatedTopLevelComments = commentFaker.Generate(COMMENTS_PER_POST);
            topLevelComments.AddRange(generatedTopLevelComments);
            comments.AddRange(generatedTopLevelComments);

            // (Tùy chọn) Tạo các bình luận trả lời (cấp 2)
            foreach (var parentComment in topLevelComments)
            {
                // Mỗi bình luận cấp 1 có 0-2 trả lời
                if (faker.Random.Bool(0.5f))
                {
                    var replyFaker = new Faker<PostComments>("vi")
                        .RuleFor(c => c.CommentID, f => commentIdCounter++)
                        .RuleFor(c => c.PostID, post.PostID)
                        .RuleFor(c => c.UserID, f => f.PickRandom(memberIdsInGroup))
                        .RuleFor(c => c.Content, f => f.Lorem.Sentence())
                        .RuleFor(c => c.ParentCommentID, parentComment.CommentID) // Gán ID bình luận cha
                        .RuleFor(c => c.CreatedAt, f => f.Date.Between(parentComment.CreatedAt, DateTime.UtcNow));

                    comments.AddRange(replyFaker.Generate(faker.Random.Int(1, 2)));
                }
            }
        }

        // --- 6. TẠO CONTENT REPORTS (đã cải tiến) ---
        var reports = new List<ContentReport>();
        var reportFaker = new Faker<ContentReport>("vi");

        for (int i = 0; i < NUMBER_OF_REPORTS; i++)
        {
            var reportedContentType = faker.PickRandom<EnumReportedContentType>();
            Guid reporterUserId;
            Guid contentOwnerId;
            Guid groupId;
            int contentId;
            string reason = faker.Lorem.Sentence();

            if (reportedContentType == EnumReportedContentType.Post && posts.Any())
            {
                // Chọn một bài viết ngẫu nhiên để báo cáo
                var randomPost = faker.PickRandom(posts);
                contentId = randomPost.PostID;
                groupId = randomPost.GroupID;
                contentOwnerId = randomPost.AuthorUserID;

                // Người báo cáo phải là một thành viên khác trong nhóm
                var potentialReporters = groupMembers
                    .Where(gm => gm.GroupID == groupId && gm.UserID != contentOwnerId)
                    .Select(gm => gm.UserID).ToList();

                if (!potentialReporters.Any()) continue; // Bỏ qua nếu không tìm thấy ai để báo cáo
                reporterUserId = faker.PickRandom(potentialReporters);
            }
            else if (reportedContentType == EnumReportedContentType.Comment && comments.Any())
            {
                // Chọn một bình luận ngẫu nhiên để báo cáo
                var randomComment = faker.PickRandom(comments);
                contentId = randomComment.CommentID;
                contentOwnerId = randomComment.UserID;

                // Tìm groupId từ bài viết cha
                var parentPost = posts.First(p => p.PostID == randomComment.PostID);
                groupId = parentPost.GroupID;

                var potentialReporters = groupMembers
                    .Where(gm => gm.GroupID == groupId && gm.UserID != contentOwnerId)
                    .Select(gm => gm.UserID).ToList();

                if (!potentialReporters.Any()) continue;
                reporterUserId = faker.PickRandom(potentialReporters);
            }
            else
            {
                continue; // Bỏ qua nếu không có nội dung để báo cáo
            }

            var report = new ContentReport
            {
                ContentReportID = i + 1,
                ReportedContentType = reportedContentType,
                ReportedContentID = contentId,
                ReportedContentOwnerId = contentOwnerId, // Bổ sung trường quan trọng
                ReportedByUserID = reporterUserId,
                GroupID = groupId,
                Reason = reason,
                Status = faker.PickRandom<EnumContentReportStatus>(),
                CreatedAt = faker.Date.Recent()
            };
            reports.Add(report);
        }


        // --- 5. TẠO MESSAGES (chỉ cho nhóm Chat) ---
        var messages = new List<Messages>();
        foreach (var conv in conversations)
        {
            var membersInConv = conversationParticipants.Where(p => p.ConversationID == conv.ConversationID).Select(p => p.UserID).ToList();
            var usersInConv = users.Where(u => membersInConv.Contains(u.Id)).ToList();
            if (!usersInConv.Any()) continue;

            var messageFaker = new Faker<Messages>("vi")
                .RuleFor(m => m.Id, f => ObjectId.GenerateNewId().ToString())
                .RuleFor(m => m.ConversationId, conv.ConversationID)
                .RuleFor(m => m.Sender, f =>
                {
                    var senderUser = f.PickRandom(usersInConv);
                    return new SenderInfo { UserId = senderUser.Id, DisplayName = senderUser.FullName!, AvatarUrl = senderUser.AvatarUrl };
                })
                .RuleFor(m => m.Content, f => f.Lorem.Sentences(f.Random.Int(1, 4)))
                .RuleFor(m => m.MessageType, EnumMessageType.Text)
                .RuleFor(m => m.SentAt, f => f.Date.Recent(30));

            messages.AddRange(messageFaker.Generate(MESSAGES_PER_CONVERSATION));
        }

        return new GeneratedData
        {
            Users = users,
            Groups = groups,
            GroupMembers = groupMembers,
            Conversations = conversations,
            ConversationParticipants = conversationParticipants,
            Posts = posts,
            Messages = messages,
            Comments = comments,
            Reports = reports
        };
    }
}
public class GeneratedData
{
    public List<AppUser> Users { get; set; } = new();
    public List<Group> Groups { get; set; } = new();
    public List<GroupMember> GroupMembers { get; set; } = new();
    public List<Conversation> Conversations { get; set; } = new();
    public List<ConversationParticipants> ConversationParticipants { get; set; } = new();
    public List<Posts> Posts { get; set; } = new();
    public List<PostComments> Comments { get; set; } = new();
    public List<ContentReport> Reports { get; set; } = new();
    public List<Messages> Messages { get; set; } = new();
}
