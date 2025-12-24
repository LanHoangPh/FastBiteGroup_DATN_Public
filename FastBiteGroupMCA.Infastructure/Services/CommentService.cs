using FastBiteGroupMCA.Application.DTOs.Comment;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.User;

namespace FastBiteGroupMCA.Infastructure.Services;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<CommentService> _logger;

    public CommentService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IMapper mapper, ILogger<CommentService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<PostCommentDTO>>> GetCommentRepliesAsync(int parentCommentId, GetCommentQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PagedResult<PostCommentDTO>>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        try
        {
            var parentComment = await _unitOfWork.PostComments.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentID == parentCommentId && !c.IsDeleted);

            if (parentComment == null)
                return ApiResponse<PagedResult<PostCommentDTO>>.Fail("COMMENT_NOT_FOUND", "Không tìm thấy bình luận cha.", 404);

            var userMembership = await _unitOfWork.GroupMembers.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(gm => gm.GroupID == parentComment.Post.GroupID && gm.UserID == userId);

            if (userMembership == null)
                return ApiResponse<PagedResult<PostCommentDTO>>.Fail("FORBIDDEN", "Bạn không có quyền xem các bình luận này.", 403);

            var currentUserRole = userMembership.Role;

            var repliesQuery = _unitOfWork.PostComments.GetQueryable()
                .Where(r => r.ParentCommentID == parentCommentId && !r.IsDeleted)
                .OrderBy(r => r.CreatedAt);

            var pagedResult = await repliesQuery
                .Select(r => new PostCommentDTO
                {
                    CommentId = r.CommentID,
                    Content = r.Content,
                    Author = _mapper.Map<PostAuthorDTO>(r.User), 
                    CreatedAt = r.CreatedAt,
                    ReplyCount = r.Replies.Count(reply => !reply.IsDeleted),
                    CanEdit = r.UserID == userId,
                    CanDelete = r.UserID == userId || currentUserRole > EnumGroupRole.Member
                })
                .ToPagedResultAsync(query.PageNumber, query.PageSize);

            return ApiResponse<PagedResult<PostCommentDTO>>.Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách trả lời cho bình luận {ParentCommentId}", parentCommentId);
            return ApiResponse<PagedResult<PostCommentDTO>>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }
}
