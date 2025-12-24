using FastBiteGroupMCA.Application.DTOs.Poll;
using FastBiteGroupMCA.Application.DTOs.Post;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices;

public interface IPollService
{
    Task<ApiResponse<CreatePollResponseDTO>> CreatePollAsync(int conversationId, CreatePollDTO dto);
    /// <summary>
    /// Xử lý hành động bỏ phiếu, thay đổi hoặc rút phiếu cho một cuộc bình chọn.
    /// </summary>
    Task<ApiResponse<object>> CastVoteAsync(int pollId, int pollOptionId);
    Task<ApiResponse<PollDetailDTO>> GetPollDetailsAsync(int pollId);
    Task<ApiResponse<object>> ClosePollAsync(int pollId);
    Task<ApiResponse<object>> DeletePollAsync(int pollId);
}
