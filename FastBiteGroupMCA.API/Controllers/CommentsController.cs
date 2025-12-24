using FastBiteGroupMCA.Application.DTOs.Comment;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers
{
    [ApiController]
    [Route("api/v1/comments")]
    [Authorize]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Public-v1")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IPostService _postService;

        public CommentsController(ICommentService commentService, IPostService postService)
        {
            _commentService = commentService;
            _postService = postService;
        }

        /// <summary>
        /// Lấy danh sách các bình luận trả lời (replies) của một bình luận cha (có phân trang).
        /// </summary>
        /// <param name="commentId">ID của bình luận cha.</param>
        /// <param name="query">Các tham số phân trang.</param>
        [HttpGet("{commentId:int}/replies")]
        [Tags("Posts.Comments")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PostCommentDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetReplies(int commentId, [FromQuery] GetCommentQuery query)
        {
            var response = await _commentService.GetCommentRepliesAsync(commentId, query);

            if (!response.Success)
            {
                return response.Errors?.FirstOrDefault()?.ErrorCode switch
                {
                    "COMMENT_NOT_FOUND" => NotFound(response),
                    "FORBIDDEN" => Forbid(),
                    _ => BadRequest(response)
                };
            }

            return Ok(response);
        }
        /// <summary>
        /// Thêm một bình luận mới vào bài viết.
        /// </summary>
        [HttpPost("{postId:int}/comments")]
        [Tags("Posts.Comments")]
        public async Task<IActionResult> AddComment(int postId, [FromBody] CreateCommentDTO dto)
        {
            var response = await _postService.AddCommentAsync(postId, dto);

            if (!response.Success)
            {
                var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
                if (errorCode == "Forbidden") return Forbid();
                if (errorCode == "PostNotFound" || errorCode == "ParentCommentNotFound") return NotFound(response);
                return BadRequest(response);
            }

            return CreatedAtAction("GetPostById", "Posts", new { postId = postId }, response);
        }

        /// <summary>
        /// Cập nhật nội dung của một bình luận.
        /// </summary>
        [HttpPut("{commentId:int}")]
        [Tags("Posts.Comments")]
        public async Task<IActionResult> UpdateComment(int commentId, [FromBody] UpdateCommentDto dto)
        {
            var response = await _postService.UpdateCommentAsync(commentId, dto);
            if (!response.Success)
            {
                var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
                if (errorCode == "Forbidden") return Forbid();
                if (errorCode == "CommentNotFound") return NotFound(response);
                return BadRequest(response);
            }
            return Ok(response);
        }

        /// <summary>
        /// Xóa một bình luận.
        /// </summary>
        [HttpDelete("{commentId:int}")]
        [Tags("Posts.Comments")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var response = await _postService.DeleteCommentAsync(commentId);

            return NoContent(); // Trả về 204 No Content khi xóa thành công
        }
    }
}
