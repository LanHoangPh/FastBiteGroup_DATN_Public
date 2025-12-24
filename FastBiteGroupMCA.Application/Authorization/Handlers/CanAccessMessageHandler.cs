using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FastBiteGroupMCA.Application.Authorization.Handlers;

public class CanAccessMessageHandler : AuthorizationHandler<CanAccessMessageRequirement>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagesRepository _messagesRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUser _currentUser;

    public CanAccessMessageHandler(IUnitOfWork unitOfWork, IMessagesRepository messagesRepository,IHttpContextAccessor httpContextAccessor, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
        _messagesRepository = messagesRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessMessageRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            context.Fail();
            return;
        }

        var messageId = _httpContextAccessor.HttpContext?.GetRouteValue("messageId")?.ToString();
        if (string.IsNullOrEmpty(messageId))
        {
            context.Fail();
            return;
        }

        // Từ messageId, truy vấn ngược ra conversationId
        var message = await _messagesRepository.GetByIdAsync(messageId);
        if (message == null)
        {
            context.Fail();
            return;
        }
        var conversationId = message.ConversationId;
        if (conversationId == null)
        {
            // Tin nhắn không thuộc về bất kỳ cuộc trò chuyện nào
            context.Fail();
            return;
        }

        // Kiểm tra xem user có phải là participant trong conversation đó không
        bool isParticipant = await _unitOfWork.ConversationParticipants.GetQueryable()
            .AnyAsync(p => p.ConversationID == conversationId && p.UserID == userId);

        if (isParticipant)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
