using FastBiteGroupMCA.Application.DTOs.VideoCall;
using FastBiteGroupMCA.Application.Response;
using Livekit.Server.Sdk.Dotnet;

namespace FastBiteGroupMCA.Application.IServices;

public interface IVideoCallService
{
    //---- Logic call mới nhất ----

    /// <summary>
    /// Bắt đầu một cuộc gọi video.
    /// </summary>
    /// <param name="conversationId"></param>
    /// <returns></returns>
    Task<ApiResponse<StartCallResponseDto>> StartCallAsync(int conversationId);

    /// <summary>
    /// Cho phép người dùng hiện tại tham gia vào một phiên gọi đang diễn ra.
    /// </summary>
    /// <param name="videoCallSessionId">ID của phiên gọi cần tham gia.</param>
    Task<ApiResponse<JoinCallResponseDto>> JoinCallGroupAsync(Guid videoCallSessionId);

    /// <summary>
    /// Ghi nhận hành động người dùng rời khỏi một phiên gọi.
    /// </summary>
    /// <param name="videoCallSessionId">ID của phiên gọi đã rời.</param>
    Task<ApiResponse<object>> LeaveCallGroupAsync(Guid videoCallSessionId);

    /// <summary>
    /// Lấy lịch sử cuộc gọi cho một cuộc trò chuyện cụ thể.
    /// </summary>
    /// <param name="conversationId"></param>
    /// <returns></returns>
    Task<ApiResponse<PagedResult<CallHistoryItemDTO>>> GetCallHistoryAsync(int conversationId, GetCallHistoryQuery query);

    /// <summary>
    /// Tắt một track media (mic, cam, screenshare) của một người tham gia.
    /// </summary>
    Task<ApiResponse<object>> MuteParticipantTrackAsync(Guid videoCallSessionId, Guid targetUserId, TrackSource source);

    /// <summary>
    /// Xóa một người tham gia khỏi cuộc gọi.
    /// </summary>
    Task<ApiResponse<object>> RemoveParticipantAsync(Guid videoCallSessionId, Guid targetUserId);

    /// <summary>
    /// Kết thúc cuộc gọi cho tất cả người tham gia trong một phiên gọi.
    /// </summary>
    /// <param name="videoCallSessionId"></param>
    /// <returns></returns>
    Task<ApiResponse<object>> EndCallForAllAsync(Guid videoCallSessionId);

    /// <summary>
    /// Chấp nhận cuộc gọi trực tiếp từ một người dùng khác.
    /// </summary>
    /// <param name="videoCallSessionId"></param>
    /// <returns></returns>
    Task<ApiResponse<AcceptCallResponseDTO>> AcceptDirectCallAsync(Guid videoCallSessionId);
    /// <summary>
    /// Từ chối cuộc gọi trực tiếp từ một người dùng khác.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    Task<ApiResponse<object>> DeclineDirectCallAsync(Guid sessionId);
    /// <summary>
    /// Ghi nhận người dùng rời khỏi cuộc gọi trực tiếp.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    Task<ApiResponse<object>> LeaveCallAsync(Guid sessionId);

    /// <summary>
    /// Xử lý timeout cho cuộc gọi video.
    /// </summary>
    /// <param name="videoCallSessionId"></param>
    /// <returns></returns>
    Task HandleCallTimeout(Guid videoCallSessionId);
}
