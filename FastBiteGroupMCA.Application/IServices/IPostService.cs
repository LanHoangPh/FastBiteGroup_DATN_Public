using FastBiteGroupMCA.Application.DTOs;
using FastBiteGroupMCA.Application.DTOs.Comment;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Post;
using FastBiteGroupMCA.Application.Response;
namespace FastBiteGroupMCA.Application.IServices;

public interface IPostService
{
    Task<ApiResponse<object>> UpdatePinStatusAsync(int postId, UpdatePinStatusDto dto);
    Task<ApiResponse<object>> DeletePostAsync(int postId);
    Task<ApiResponse<PostDetailDTO>> UpdatePostAsync(int postId, UpdatePostDto dto);
    Task<ApiResponse<object>> DeleteCommentAsync(int commentId);
    Task<ApiResponse<PostCommentDTO>> UpdateCommentAsync(int commentId, UpdateCommentDto dto);
    Task<ApiResponse<PagedResult<PostSummaryDTO>>> GetPostsForGroupAsync(Guid groupId, GetPostsInGroupQuery query);
    Task<ApiResponse<PostDetailDTO>> CreatePostAsync(Guid groupId, CreatePostDTO dto);
    Task<ApiResponse<LikePostResponseDTO>> ToggleLikePostAsync(int postId);
    Task<ApiResponse<PostDetailDTO>> GetPostByIdAsync(int postId, GetPostCommentsQuery query);
    Task<ApiResponse<PostCommentDTO>> AddCommentAsync(int postId, CreateCommentDTO dto);
}
