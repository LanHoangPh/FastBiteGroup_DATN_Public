using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FastBiteGroupMCA.Infastructure.Hubs;

[Authorize(Roles = "Customer,VIP")]
public class PostsHub : Hub
{
    private readonly ILogger<PostsHub> _logger;
    public PostsHub(ILogger<PostsHub> logger) { _logger = logger; }

    /// <summary>
    /// Client sẽ gọi phương thức này khi bắt đầu xem một bài viết.
    /// </summary>
    /// <param name="postId">ID của bài viết đang xem.</param>
    public async Task JoinPostChannel(int postId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetPostGroupName(postId));
        _logger.LogInformation("Client {ConnectionId} joined channel for post {PostId}", Context.ConnectionId, postId);
    }

    /// <summary>
    /// Client sẽ gọi phương thức này khi không còn xem bài viết đó nữa.
    /// </summary>
    /// <param name="postId">ID của bài viết đã xem xong.</param>
    public async Task LeavePostChannel(int postId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetPostGroupName(postId));
        _logger.LogInformation("Client {ConnectionId} left channel for post {PostId}", Context.ConnectionId, postId);
    }

    // Helper method để đảm bảo tên group luôn nhất quán
    private static string GetPostGroupName(int postId) => $"post-updates_{postId}";
}
