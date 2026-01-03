using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FastBiteGroupMCA.Application.Authorization.Handlers;
/// <summary>
/// Handler này xử lý các quyền dựa trên quyền sở hữu nội dung.
/// Nó không quan tâm đến vai trò của người dùng trong nhóm, chỉ quan tâm "có phải của bạn không?".
/// </summary>
public class ContentManagementHandler : AuthorizationHandler<ManageContentRequirement>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagesRepository _messagesRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUser _currentUser;

    public ContentManagementHandler(IUnitOfWork unitOfWork, IMessagesRepository messagesRepository,IHttpContextAccessor httpContextAccessor, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
        _messagesRepository = messagesRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageContentRequirement requirement)
    {
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

        Guid? ownerId = null;

        if (int.TryParse(httpContext.GetRouteValue("postId")?.ToString(), out var postId))
        {
            ownerId = await _unitOfWork.Posts.GetQueryable()
                .Where(p => p.PostID == postId)
                .Select(p => (Guid?)p.AuthorUserID)
                .FirstOrDefaultAsync();
        }
        else if (httpContext.GetRouteValue("messageId")?.ToString() is string messageId && !string.IsNullOrEmpty(messageId))
        {
            var message = await _messagesRepository.GetByIdAsync(messageId);

            if (message != null && Guid.TryParse(message.Sender?.UserId.ToString(), out var senderIdGuid))
            {
                ownerId = senderIdGuid;
            }
        }

        if (ownerId.HasValue && ownerId.Value == userId)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
