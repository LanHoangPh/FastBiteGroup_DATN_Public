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

        Guid? ownerId = null;

        // 2. Xác định loại tài nguyên (Post hay Message) từ route và lấy ID chủ sở hữu
        if (int.TryParse(httpContext.GetRouteValue("postId")?.ToString(), out var postId))
        {
            // Truy vấn ID tác giả của bài đăng qua Unit of Work
            ownerId = await _unitOfWork.Posts.GetQueryable()
                .Where(p => p.PostID == postId)
                .Select(p => (Guid?)p.AuthorUserID) // Ép kiểu sang nullable Guid để nhất quán
                .FirstOrDefaultAsync();
        }
        else if (httpContext.GetRouteValue("messageId")?.ToString() is string messageId && !string.IsNullOrEmpty(messageId))
        {
            var message = await _messagesRepository.GetByIdAsync(messageId);

            // Lấy trực tiếp SenderUserID từ document Message
            if (message != null && Guid.TryParse(message.Sender?.UserId.ToString(), out var senderIdGuid))
            {
                ownerId = senderIdGuid;
            }
        }

        // 3. So sánh ID chủ sở hữu với ID người dùng hiện tại
        if (ownerId.HasValue && ownerId.Value == userId)
        {
            // Nếu khớp, người dùng có quyền (Edit, Delete,...) trên nội dung của họ
            context.Succeed(requirement);
        }
        else
        {
            // Nếu không khớp hoặc không tìm thấy tài nguyên, từ chối quyền
            context.Fail();
        }
    }
}
