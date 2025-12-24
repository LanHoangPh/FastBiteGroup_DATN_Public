using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FastBiteGroupMCA.Infastructure.Hubs;

[Authorize]
public class VideoCallHub : Hub
{
    // Override OnConnectedAsync để có thể log hoặc xử lý khi client kết nối
    public override async Task OnConnectedAsync()
    {
        // Có thể thêm các logic cần thiết khi một user kết nối vào hệ thống signaling
        // Ví dụ: tham gia một group chung cho các thông báo video call
        // await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);
        await base.OnConnectedAsync();
    }

    // Các phương thức khác có thể được thêm vào sau này nếu client cần chủ động
    // gửi tín hiệu lên server, ví dụ: "AnswerCall", "DeclineCall".
}
