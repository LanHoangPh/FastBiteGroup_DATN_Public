using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using FastBiteGroupMCA.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FastBiteGroupMCA.Application.Authorization.Handlers;
/// <summary>
/// Hander này là để kiểmt ra quyền của thành viên trong Group để thực hiện các hành động quản lý nhóm tương ứng với các role như admin, moderator, member.
/// </summary>
public class GroupManagementHandler : AuthorizationHandler<ManageGroupRequirement>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagesRepository _messagesRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUser _currentUser;

    public GroupManagementHandler(IUnitOfWork unitOfWork, IMessagesRepository messagesRepository, IHttpContextAccessor httpContextAccessor, ICurrentUser currentUser)
    { 
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
        _messagesRepository = messagesRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageGroupRequirement requirement)
    {
        // 1. Kiểm tra người dùng đã xác thực và có ID hợp lệ chưa
        if (!_currentUser.IsAuthenticated || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            context.Fail();
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return;
        }

        var groupId = await GetGroupIdFromHttpContextAsync(httpContext);
        if (!groupId.HasValue)
        {
            context.Fail();
            return;
        }

        var member = await _unitOfWork.GroupMembers.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(gm => gm.GroupID == groupId.Value && gm.UserID == userId);

        if (member == null)
        {
            context.Fail();
            return;
        }

        //  Rẽ nhánh logic cho từng hoạt động, xử lý Fail tường minh
        switch (requirement.Name)
        {
            case nameof(GroupOperations.View):
            case nameof(GroupOperations.CreateContent):
                context.Succeed(requirement); // Bất kỳ thành viên nào cũng có quyền
                break;

            case nameof(GroupOperations.ModerateContent):
                if (member.Role == EnumGroupRole.Admin || member.Role == EnumGroupRole.Moderator)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            case nameof(GroupOperations.EditInfo):
            case nameof(GroupOperations.ManageRoles):
            case nameof(GroupOperations.DeleteGroup):
                if (member.Role == EnumGroupRole.Admin)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            case nameof(GroupOperations.RemoveMember):
                if (!Guid.TryParse(httpContext.GetRouteValue("userId")?.ToString(), out var targetUserId))
                {
                    context.Fail();
                    break;
                }

                var target = await _unitOfWork.GroupMembers.GetQueryable().AsNoTracking()
                    .FirstOrDefaultAsync(gm => gm.GroupID == groupId.Value && gm.UserID == targetUserId);

                if (target == null)
                {
                    context.Fail(); 
                    break;
                }

                if (member.Role == EnumGroupRole.Admin ||
                   (member.Role == EnumGroupRole.Moderator && target.Role == EnumGroupRole.Member))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
                break;

            default:
                context.Fail(); // Nếu không có hoạt động nào khớp
                break;
        }
    }

    /// <summary>
    /// Helper để lấy GroupID một cách linh hoạt từ các loại route khác nhau.
    /// </summary>
    private async Task<Guid?> GetGroupIdFromHttpContextAsync(HttpContext httpContext)
    {
        //lấy trực tiếp từ route /api/groups/{groupId}
        if (Guid.TryParse(httpContext.GetRouteValue("groupId")?.ToString(), out var groupId))
        {
            return groupId;
        }

        // lấy qua postId từ route /api/posts/{postId}
        if (int.TryParse(httpContext.GetRouteValue("postId")?.ToString(), out var postId))
        {
            return await _unitOfWork.Posts.GetQueryable()
                .Where(p => p.PostID == postId)
                .Select(p => p.GroupID)
                .FirstOrDefaultAsync();
        }

        // lấy qua messageId từ route /api/messages/{messageId}
        if (httpContext.GetRouteValue("messageId")?.ToString() is string messageId && !string.IsNullOrEmpty(messageId))
        {
            //Truy vấn MongoDB để lấy tin nhắn
            var message = await _messagesRepository.GetByIdAsync(messageId);
            if (message == null) return null;

            // Dùng conversationId từ tin nhắn để truy vấn SQL và tìm GroupId
            var conversation = await _unitOfWork.Conversations.GetQueryable()
                .Where(c => c.ConversationID == message.ConversationId)
                .Select(c => new { c.ExplicitGroupID })
                .FirstOrDefaultAsync();

            return conversation?.ExplicitGroupID;
        }

        return null; 
    }
}
