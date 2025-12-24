using FastBiteGroupMCA.Application.DTOs.Comment;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Post;
using FastBiteGroupMCA.Application.IServices;
using FastBiteGroupMCA.Infastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers;

[ApiController]
[Route("api/v1/posts")]
[Produces("application/json")]
[Authorize]
[ApiExplorerSettings(GroupName = "Public-v1")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    /// <summary>
    /// Thích hoặc bỏ thích một bài viết.
    /// </summary>
    /// <param name="postId">ID của bài viết.</param>
    [HttpPost("{postId:int}/toggle-like")]
    [Tags("Posts.Interactions")]
    public async Task<IActionResult> LikePost(int postId)
    {
        var response = await _postService.ToggleLikePostAsync(postId);

        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "PostNotFound") return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }
    /// <summary>
    /// Lấy thông tin chi tiết của một bài viết theo ID.
    /// </summary>
    /// <remarks>
    /// Trả về thông tin bài viết và trang đầu tiên của các bình luận cấp 1.
    /// </remarks>
    /// <param name="postId">ID của bài viết.</param>
    /// <param name="query">Các tham số phân trang cho danh sách bình luận (ví dụ: pageNumber, pageSize).</param>
    [HttpGet("{postId:int}")]
    [Tags("Posts.Management")]
    public async Task<IActionResult> GetPostById(int postId, [FromQuery] GetPostCommentsQuery query)
    {
        // Gọi đến service với đầy đủ tham số
        var response = await _postService.GetPostByIdAsync(postId, query);

        if (!response.Success)
        {
            // Phần xử lý lỗi của bạn đã rất tốt và đúng chuẩn, chúng ta sẽ giữ nguyên nó.
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "Forbidden") return Forbid();
            if (errorCode == "PostNotFound") return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Cập nhật thông tin một bài viết.
    /// </summary>
    /// <param name="postId"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPut("{postId:int}")]
    [Tags("Posts.Management")]
    public async Task<IActionResult> UpdatePost(int postId, [FromBody] UpdatePostDto dto)
    {
        var response = await _postService.UpdatePostAsync(postId, dto);
        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "Forbidden") return Forbid();
            if (errorCode == "PostNotFound") return NotFound(response);
            return BadRequest(response);
        }
        return Ok(response);
    }

    /// <summary>
    /// Xóa một bài viết.
    /// </summary>
    /// <param name="postId"></param>
    /// <returns></returns>
    [HttpDelete("{postId:int}")]
    [Tags("Posts.Management")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        var response = await _postService.DeletePostAsync(postId);
        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "Forbidden") return Forbid();
            if (errorCode == "PostNotFound") return NotFound(response);
            return BadRequest(response);
        }
        return NoContent(); // 204 No Content khi xóa thành công
    }

    /// <summary>
    /// Ghim một bài viết lên đầu danh sách bài viết. 
    /// </summary>
    /// <param name="postId"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPut("{postId:int}/pin")]
    [Tags("Posts.Management")]
    public async Task<IActionResult> UpdatePinStatus(int postId, [FromBody] UpdatePinStatusDto dto)
    {
        var response = await _postService.UpdatePinStatusAsync(postId, dto);
        if (!response.Success)
        {
            var errorCode = response.Errors?.FirstOrDefault()?.ErrorCode;
            if (errorCode == "Forbidden") return Forbid();
            if (errorCode == "PostNotFound") return NotFound(response);
            return BadRequest(response);
        }
        return NoContent();
    }

}
