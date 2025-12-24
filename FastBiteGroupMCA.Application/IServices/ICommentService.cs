using FastBiteGroupMCA.Application.DTOs.Comment;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.Response;

namespace FastBiteGroupMCA.Application.IServices
{
    public interface ICommentService
    {
        Task<ApiResponse<PagedResult<PostCommentDTO>>> GetCommentRepliesAsync(int parentCommentId, GetCommentQuery query);
    }
}
