using FastBiteGroupMCA.Application.DTOs.Hubs;
using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.IServices.BackgroundJob;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FastBiteGroupMCA.Infastructure.Hubs;
[Authorize] // Chỉ cho phép người dùng đã xác thực kết nối đến Hub này
public class ChatHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatService _chatService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;

    public ChatHub(
        IUnitOfWork unitOfWork,
        IChatService chatService,
        IBackgroundJobClient backgroundJobClient,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _chatService = chatService;
        _backgroundJobClient = backgroundJobClient;
        _currentUser = currentUser;
        _mapper = mapper;
    }
    public override Task OnConnectedAsync() => base.OnConnectedAsync();
    public override Task OnDisconnectedAsync(Exception? exception) => base.OnDisconnectedAsync(exception);
    public async Task JoinConversation(int conversationId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId)) return;

        // Chỉ cho phép user tham gia group SignalR NẾU họ thực sự là thành viên
        var isParticipant = await _unitOfWork.ConversationParticipants.GetQueryable()
            .AnyAsync(p => p.ConversationID == conversationId && p.UserID == userId);

        if (isParticipant)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }
    }
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
    }
    public async Task StartTyping(int conversationId, TypingUserDto typingUser)
    {
        // Hub chỉ cần broadcast thông tin mà client đã gửi
        await Clients.OthersInGroup($"conversation_{conversationId}")
                     .SendAsync("UserIsTyping", conversationId, typingUser);
    }

    public async Task StopTyping(int conversationId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId)) return;
        // Gửi ID của người dùng đã ngừng gõ
        await Clients.OthersInGroup($"conversation_{conversationId}")
                     .SendAsync("UserStoppedTyping", conversationId, userId);
    }
    /// <summary>
    /// Client gọi phương thức này để báo server rằng các tin nhắn đã được đọc.
    /// </summary>
    public Task MarkMessagesAsRead(MarkAsReadDTO dto)
    {
        if (!Guid.TryParse(Context.UserIdentifier, out var userId))
            return Task.CompletedTask;

        _backgroundJobClient.Enqueue<IReadReceiptProcessor>(processor =>
            processor.ProcessAsync(dto, userId)
        );

        return Task.CompletedTask; // Hub trả về ngay lập tức
    }
}
