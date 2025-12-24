using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DTOs.VideoCall;
using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FastBiteGroupMCA.Application.Authorization.Handlers;

public class IsConversationParticipantHandler : AuthorizationHandler<IsConversationParticipantRequirement>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUser _currentUser;

    public IsConversationParticipantHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsConversationParticipantRequirement requirement)
    {
        int conversationId;
        var httpContext = _httpContextAccessor.HttpContext;

        if (int.TryParse(httpContext?.GetRouteValue("conversationId")?.ToString(), out var idFromRoute))
        {
            conversationId = idFromRoute;
        }
        else
        {
            // đọc body request để lấy conversationId
            httpContext?.Request.EnableBuffering();
            using var reader = new StreamReader(httpContext!.Request.Body, leaveOpen: true);
            var bodyAsString = await reader.ReadToEndAsync();
            var requestDto = JsonSerializer.Deserialize<JoinRoomRequestDTO>(bodyAsString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            httpContext.Request.Body.Position = 0; // reset vị trí để controller có thể đọc lại

            if (requestDto == null) { context.Fail(); return; }
            conversationId = requestDto.ConversationId;
        }
        if (!_currentUser.IsAuthenticated || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            context.Fail();
            return;
        }

        //// Lấy conversationId từ route
        //if (!int.TryParse(_httpContextAccessor.HttpContext?.GetRouteValue("conversationId")?.ToString(), out var conversationId))
        //{
        //    context.Fail();
        //    return;
        //}

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
