using AutoMapper.QueryableExtensions;
using DocumentFormat.OpenXml.Spreadsheet;
using FastBiteGroupMCA.Application.DTOs.Comment;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Post;
using FastBiteGroupMCA.Application.DTOs.SharedFile;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.Notifications.Templates;
using FastBiteGroupMCA.Infastructure.Hubs;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel;

namespace FastBiteGroupMCA.Infastructure.Services;

public class PostService : IPostService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IContentRendererService _contentRenderer;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<PostService> _logger;
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly IHubContext<PostsHub> _postsHubContext;

    public PostService(
        IUnitOfWork unitOfWork, 
        ICurrentUser currentUser, 
        IMapper mapper, 
        IHubContext<NotificationsHub> hubContext, 
        ILogger<PostService> logger, 
        IBackgroundJobClient backgroundJobClient,
        IContentRendererService contentRendererService,
        INotificationService notificationService,
        IHubContext<PostsHub> postsHubContext)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _hubContext = hubContext;
        _logger = logger;
        _contentRenderer = contentRendererService;
        _backgroundJobClient = backgroundJobClient;
        _notificationService = notificationService;
        _postsHubContext = postsHubContext;
    }

    public async Task<ApiResponse<PostDetailDTO>> CreatePostAsync(Guid groupId, CreatePostDTO dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PostDetailDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupID == groupId && gm.UserID == userId);
        if (!isMember)
            return ApiResponse<PostDetailDTO>.Fail("Forbidden", "Bạn không phải là thành viên của nhóm để đăng bài.");

        if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
        {
            var validFilesCount = await _unitOfWork.SharedFiles.GetQueryable()
                .CountAsync(f => dto.AttachmentFileIds.Contains(f.FileID) && f.UploadedByUserID == userId);
            if (validFilesCount != dto.AttachmentFileIds.Count)
                return ApiResponse<PostDetailDTO>.Fail("InvalidAttachments", "Một hoặc nhiều file đính kèm không hợp lệ hoặc không thuộc sở hữu của bạn.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var renderedHtml = _contentRenderer.RenderAndSanitize(dto.ContentJson);

            var newPost = new Posts
            {
                GroupID = groupId,
                AuthorUserID = userId,
                Title = dto.Title,
                ContentJson = dto.ContentJson, 
                ContentHtml = renderedHtml,  
                Status = EnumPostStatus.PendingReview, 
                CreatedAt = DateTime.UtcNow
            };

            if (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any())
            {
                newPost.Attachments = dto.AttachmentFileIds
                    .Select(fileId => new PostAttachment { FileID = fileId })
                    .ToList();
            }

            await _unitOfWork.Posts.AddAsync(newPost);
            await _unitOfWork.SaveChangesAsync();

            _backgroundJobClient.Enqueue<IContentModerationService>(
                service => service.ModeratePostAsync(newPost.PostID)
            );

            var author = await _unitOfWork.Users.GetQueryable().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var authorMembership = await _unitOfWork.GroupMembers.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);

            var createdPostForDto = await _unitOfWork.Posts.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.PostID == newPost.PostID)
                    .Include(p => p.Author)
                    .Include(p => p.Attachments)
                        .ThenInclude(pa => pa.SharedFile)
                    .FirstOrDefaultAsync();

            if (createdPostForDto == null)
            {
                return ApiResponse<PostDetailDTO>.Fail("FETCH_FAILED", "Không thể lấy lại thông tin bài viết vừa tạo.");
            }


            var emptyCommentsPage = new PagedResult<PostCommentDTO>(
                new List<PostCommentDTO>(),
                totalRecords: 0,
                pageNumber: 1,
                pageSize: 10 
            );

            var responseDto = new PostDetailDTO
            {
                PostId = createdPostForDto.PostID,
                Title = createdPostForDto.Title,
                ContentJson = createdPostForDto.ContentJson,
                ContentHtml = createdPostForDto.ContentHtml,
                Author = _mapper.Map<PostAuthorDTO>(author),
                LikeCount = 0,
                CommentCount = 0,
                CreatedAt = createdPostForDto.CreatedAt,
                IsPinned = createdPostForDto.IsPinned,
                IsLikedByCurrentUser = false, 
                Attachments = _mapper.Map<List<PostAttachmentDTO>>(createdPostForDto.Attachments),

                CanEdit = true,
                CanDelete = true,
                CanPin = (authorMembership?.Role == EnumGroupRole.Admin || authorMembership?.Role == EnumGroupRole.Moderator),
                CommentsPage = emptyCommentsPage
            };

            await transaction.CommitAsync();
            return ApiResponse<PostDetailDTO>.Ok(responseDto, "Bài viết của bạn đã được tạo và đang được xử lý.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi tạo bài viết cho nhóm {GroupId} bởi người dùng {UserId}", groupId, userId);
            return ApiResponse<PostDetailDTO>.Fail("ServerError", "Có lỗi xảy ra khi tạo bài viết.");
        }
    }

    public async Task<ApiResponse<PostDetailDTO>> GetPostByIdAsync(int postId, GetPostCommentsQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PostDetailDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        var postDetail = await _unitOfWork.Posts.GetQueryable().Include(p => p.Likes).Include(p => p.Comments).Include(p => p.Attachments).ThenInclude(pa => pa.SharedFile)
            .Where(p => p.PostID == postId && !p.IsDeleted)
            .Select(p => new
            {
                Post = p,
                Author = p.Author,
                Group = p.Group,
                CurrentUserRole = p.Group.Members.Where(m => m.UserID == userId).Select(m => (EnumGroupRole?)m.Role).FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (postDetail == null)
            return ApiResponse<PostDetailDTO>.Fail("PostNotFound", "Không tìm thấy bài viết.");

        if (postDetail.CurrentUserRole == null)
            return ApiResponse<PostDetailDTO>.Fail("Forbidden", "Bạn không có quyền xem bài viết này.");

        var post = postDetail.Post;
        var currentUserRole = postDetail.CurrentUserRole.Value;
        var commentsPage = await _unitOfWork.PostComments.GetQueryable()
            .Where(c => c.PostID == postId && c.ParentCommentID == null && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new PostCommentDTO
            {
                CommentId = c.CommentID,
                Content = c.Content,
                Author = _mapper.Map<PostAuthorDTO>(post.Author),
                CreatedAt = c.CreatedAt,
                ReplyCount = c.Replies.Count(r => !r.IsDeleted),
                CanEdit = c.UserID == userId,
                CanDelete = c.UserID == userId || currentUserRole > EnumGroupRole.Member
            })
            .ToPagedResultAsync(query.PageNumber, query.PageSize);

        var postDetailDto = new PostDetailDTO
        {
            PostId = post.PostID,
            Title = post.Title,
            ContentJson = post.ContentJson,
            ContentHtml = post.ContentHtml,
            Author = _mapper.Map<PostAuthorDTO>(post.Author),
            LikeCount = post.Likes.Count(),
            CommentCount = post.Comments.Count(c => !c.IsDeleted),
            CreatedAt = post.CreatedAt,
            IsPinned = post.IsPinned,
            IsLikedByCurrentUser = post.Likes.Any(l => l.UserID == userId),
            Attachments = _mapper.Map<List<PostAttachmentDTO>>(post.Attachments),

            CanEdit = post.AuthorUserID == userId,
            CanDelete = post.AuthorUserID == userId || currentUserRole > EnumGroupRole.Member,
            CanPin = currentUserRole > EnumGroupRole.Member,
            CommentsPage = commentsPage
        };

        return ApiResponse<PostDetailDTO>.Ok(postDetailDto);
    }

    public async Task<ApiResponse<PagedResult<PostSummaryDTO>>> GetPostsForGroupAsync(Guid groupId, GetPostsInGroupQuery query)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PagedResult<PostSummaryDTO>>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        var currentUserMembership = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupID == groupId && gm.UserID == userId);

        if (currentUserMembership == null)
            return ApiResponse<PagedResult<PostSummaryDTO>>.Fail("Forbidden", "Bạn không phải là thành viên của nhóm này để xem bài viết.");

        var currentUserRole = currentUserMembership.Role;

        try
        {
            var baseQuery = _unitOfWork.Posts.GetQueryable().Include(p=> p.Attachments).ThenInclude(p => p.SharedFile)
                .Where(p =>
                    p.GroupID == groupId &&
                    !p.IsDeleted &&
                    p.Status == EnumPostStatus.Published
                );

            if (query.MyPostsOnly) 
            {
                baseQuery = baseQuery.Where(p => p.AuthorUserID == userId);
            }
            else if (query.AuthorId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.AuthorUserID == query.AuthorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTermTrimmed = query.SearchTerm.Trim();
                baseQuery = baseQuery.Where(p => p.Title != null && p.Title.Contains(searchTermTrimmed));
            }

            var orderedQuery = baseQuery.OrderByDescending(p => p.IsPinned);
            orderedQuery = query.SortBy?.ToLower() switch
            {
                "popular" => orderedQuery.ThenByDescending(p => p.Likes.Count() + p.Comments.Count()),
                _ => orderedQuery.ThenByDescending(p => p.CreatedAt)
            };

            var pagedResult = await orderedQuery
            .Select(p => new PostSummaryDTO
            {
                PostId = p.PostID,
                Title = p.Title,
                Author = new PostAuthorDTO
                {
                    UserId = p.AuthorUserID,
                    FullName = p.Author.FullName,
                    AvatarUrl = p.Author.AvatarUrl
                },

                LikeCount = p.Likes.Count(),
                CommentCount = p.Comments.Count(c => !c.IsDeleted),
                CreatedAt = p.CreatedAt,
                IsPinned = p.IsPinned,
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserID == userId),
                Attachments = _mapper.Map<List<PostAttachmentDTO>>(p.Attachments),

                CanEdit = p.AuthorUserID == userId,
                CanDelete = p.AuthorUserID == userId || currentUserRole == EnumGroupRole.Admin || currentUserRole == EnumGroupRole.Moderator,
                CanPin = currentUserRole == EnumGroupRole.Admin || currentUserRole == EnumGroupRole.Moderator
            })
            .ToPagedResultAsync(query.PageNumber, query.PageSize);

            return ApiResponse<PagedResult<PostSummaryDTO>>.Ok(pagedResult, "Lấy danh sách bài viết thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách bài viết cho nhóm {GroupId} bởi người dùng {UserId}", groupId, userId);
            return ApiResponse<PagedResult<PostSummaryDTO>>.Fail("ServerError", "Có lỗi xảy ra khi lấy danh sách bài viết.");
        }
    }

    public async Task<ApiResponse<LikePostResponseDTO>> ToggleLikePostAsync(int postId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<LikePostResponseDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        try
        {
            var post = await _unitOfWork.Posts.GetQueryable()
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.PostID == postId && !p.IsDeleted);
            if (post == null || post.IsDeleted)
                return ApiResponse<LikePostResponseDTO>.Fail("PostNotFound", "Không tìm thấy bài viết.");

            if (!await _unitOfWork.GroupMembers.GetQueryable()
                .AnyAsync(gm => gm.GroupID == post.GroupID && gm.UserID == userId))
                return ApiResponse<LikePostResponseDTO>.Fail("Forbidden", "Bạn không có quyền tương tác với bài viết này.");

            bool isNowLiked;
            var existingLike = await _unitOfWork.PostLikes.GetQueryable()
                .FirstOrDefaultAsync(l => l.PostID == postId && l.UserID == userId);

            if (existingLike != null)
            {
                _unitOfWork.PostLikes.Remove(existingLike);
                isNowLiked = false;
            }
            else
            {
                await _unitOfWork.PostLikes.AddAsync(new PostLikes { PostID = postId, UserID = userId, CreatedAt = DateTime.UtcNow });
                isNowLiked = true;
            }

            await _unitOfWork.SaveChangesAsync();

            var newLikeCount = await _unitOfWork.PostLikes.GetQueryable().CountAsync(l => l.PostID == postId);


            _backgroundJobClient.Enqueue(() =>
                SendLikeUpdateToGroupAsync(postId, newLikeCount, userId));

            if (isNowLiked && post.AuthorUserID != userId)
            {
                var eventData = new PostLikedEventData(postId, userId);

                _backgroundJobClient.Enqueue<INotificationService>(service =>
                    service.DispatchNotificationAsync<PostLikedNotificationTemplate, PostLikedEventData>(
                        post.AuthorUserID,
                        eventData
                    )
                );
            }

            var responseDto = new LikePostResponseDTO
            {
                NewLikeCount = newLikeCount,
                IsLikedByCurrentUser = isNowLiked
            };
            return ApiResponse<LikePostResponseDTO>.Ok(responseDto, "Thao tác thành công.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thích/bỏ thích bài viết {PostId}", postId);
            return ApiResponse<LikePostResponseDTO>.Fail("ServerError", "Có lỗi xảy ra.");
        }
    }


    [DisplayName("Broadcast Like Update for Post: {1}")]
    public async Task SendLikeUpdateToGroupAsync(int postId, int newLikeCount, Guid userId)
    {
        var groupName = $"post-updates_{postId}";
        await _postsHubContext.Clients.Group(groupName)
            .SendAsync("PostLikeUpdated", newLikeCount, userId, true); 
    }

    public async Task<ApiResponse<PostCommentDTO>> AddCommentAsync(int postId, CreateCommentDTO dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PostCommentDTO>.Fail("Unauthorized", "Người dùng không hợp lệ.");

        var post = await _unitOfWork.Posts.GetByIdAsync(postId);
        if (post == null || post.IsDeleted)
            return ApiResponse<PostCommentDTO>.Fail("PostNotFound", "Không tìm thấy bài viết.");

        if (!await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupID == post.GroupID && gm.UserID == userId))
            return ApiResponse<PostCommentDTO>.Fail("Forbidden", "Bạn không có quyền bình luận trong bài viết này.");

        if (dto.ParentCommentId.HasValue)
        {
            var parentExists = await _unitOfWork.PostComments.GetQueryable()
                .AnyAsync(pc => pc.CommentID == dto.ParentCommentId.Value && pc.PostID == postId && !pc.IsDeleted);
            if (!parentExists)
                return ApiResponse<PostCommentDTO>.Fail("ParentCommentNotFound", "Bình luận cha không tồn tại hoặc đã bị xóa.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var newComment = _mapper.Map<PostComments>(dto);
            newComment.PostID = postId;
            newComment.UserID = userId;
            newComment.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.PostComments.AddAsync(newComment);

            await _unitOfWork.SaveChangesAsync();
           

            var currentUser = await _unitOfWork.Users.GetQueryable()
                .AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

            var commentDto = new PostCommentDTO
            {
                CommentId = newComment.CommentID,
                Content = newComment.Content,
                Author = _mapper.Map<PostAuthorDTO>(currentUser),
                CreatedAt = newComment.CreatedAt,
                ReplyCount = 0, 
                CanEdit = true, 
                CanDelete = true
            };

            _backgroundJobClient.Enqueue(() =>
                 BroadcastNewCommentRealtimeAsync(post.GroupID, postId, commentDto));

            if (post.AuthorUserID != userId)
            {
                var eventData = new NewCommentEventData(postId, newComment.CommentID, userId);

                _backgroundJobClient.Enqueue<INotificationService>(s =>
                    s.DispatchNotificationAsync<NewCommentNotificationTemplate, NewCommentEventData>(post.AuthorUserID, eventData));
            }

            if (newComment.ParentCommentID.HasValue)
            {
                _backgroundJobClient.Enqueue(() =>
                    NotifyParentCommentAuthorAsync(newComment.ParentCommentID.Value, post.PostID, newComment.CommentID, userId));
            }

            await transaction.CommitAsync();

            return ApiResponse<PostCommentDTO>.Ok(commentDto, "Bình luận thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi thêm bình luận vào bài viết {PostId} bởi người dùng {UserId}", postId, userId);
            return ApiResponse<PostCommentDTO>.Fail("ServerError", "Có lỗi xảy ra khi bình luận.");
        }
    }
    [DisplayName("Broadcast New Comment for Post: {1}")]
    public async Task BroadcastNewCommentRealtimeAsync(Guid groupId, int postId, PostCommentDTO newCommentDto)
    {
        var groupName = $"post-updates_{postId}";
        await _postsHubContext.Clients.Group(groupName)
            .SendAsync("NewCommentReceived", newCommentDto);
    }

    [DisplayName("Notify Parent Comment Author for Comment: {0}")]
    public async Task NotifyParentCommentAuthorAsync(int parentCommentId, int postId, int newReplyId, Guid replierId)
    {
        var parentComment = await _unitOfWork.PostComments.GetByIdAsync(parentCommentId);

        if (parentComment != null && parentComment.UserID != replierId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            var replyComment = await _unitOfWork.PostComments.GetByIdAsync(newReplyId);
            var replier = await _unitOfWork.Users.GetByIdAsync(replierId);

            if (post != null && replyComment != null && replier != null)
            {
                var eventData = new NewCommentReplyEventData(post, parentComment, replyComment, replier);
                await _notificationService.DispatchNotificationAsync<NewCommentReplyNotificationTemplate, NewCommentReplyEventData>(
                    parentComment.UserID, 
                    eventData
                );
            }
        }
    }

    public async Task<ApiResponse<object>> DeleteCommentAsync(int commentId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var comment = await _unitOfWork.PostComments.GetQueryable()
            .Include(c => c.Post)
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.CommentID == commentId && !c.IsDeleted);

        if (comment == null)
            return ApiResponse<object>.Fail("COMMENT_NOT_FOUND", "Không tìm thấy bình luận.", 404);

        var userMembership = await _unitOfWork.GroupMembers.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(gm => gm.GroupID == comment.Post.GroupID && gm.UserID == userId);

        bool isAuthor = comment.UserID == userId;
        bool isGroupAdminOrMod = userMembership != null && userMembership.Role > EnumGroupRole.Member;

        if (!isAuthor && !isGroupAdminOrMod)
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền xóa bình luận này.", 403);

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {

            var allCommentIdsToDelete = new List<int>();

            await SoftDeleteCommentAndRepliesAsync(comment, allCommentIdsToDelete);

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            _backgroundJobClient.Enqueue(() =>
                BroadcastCommentDeletionAsync(comment.PostID, allCommentIdsToDelete));

            return ApiResponse<object>.Ok(null, "Xóa bình luận thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi xóa bình luận {CommentId}", commentId);
            return ApiResponse<object>.Fail("SERVER_ERROR", "Đã có lỗi xảy ra.");
        }
    }

    private async Task SoftDeleteCommentAndRepliesAsync(PostComments comment, List<int> deletedIds)
    {
        if (!comment.IsDeleted)
        {
            comment.IsDeleted = true;
            comment.Content = "[Bình luận đã bị xóa]";
            deletedIds.Add(comment.CommentID);

            if (comment.Replies != null && comment.Replies.Any())
            {
                foreach (var reply in comment.Replies)
                {
                    var fullReply = await _unitOfWork.PostComments.GetQueryable()
                        .Include(r => r.Replies)
                        .FirstAsync(r => r.CommentID == reply.CommentID);
                    await SoftDeleteCommentAndRepliesAsync(fullReply, deletedIds);
                }
            }
        }
    }

    public async Task<ApiResponse<PostCommentDTO>> UpdateCommentAsync(int commentId, UpdateCommentDto dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PostCommentDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var comment = await _unitOfWork.PostComments.GetQueryable()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CommentID == commentId && !c.IsDeleted);

        if (comment == null)
            return ApiResponse<PostCommentDTO>.Fail("COMMENT_NOT_FOUND", "Không tìm thấy bình luận.", 404);

        if (comment.UserID != userId)
            return ApiResponse<PostCommentDTO>.Fail("FORBIDDEN", "Bạn không có quyền sửa bình luận này.", 403);

        comment.Content = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var commentDto = _mapper.Map<PostCommentDTO>(comment);

        commentDto.CanEdit = true;
        commentDto.CanDelete = true;

        _backgroundJobClient.Enqueue(() => BroadcastCommentUpdateAsync(comment.PostID, commentDto));

        return ApiResponse<PostCommentDTO>.Ok(commentDto, "Cập nhật bình luận thành công.");
    }

    [DisplayName("Broadcast Comment Update for Post: {0}")]
    public async Task BroadcastCommentUpdateAsync(int postId, PostCommentDTO updatedCommentDto)
    {
        await _postsHubContext.Clients.Group($"post-updates_{postId}")
            .SendAsync("CommentUpdated", updatedCommentDto);
    }

    [DisplayName("Broadcast Comment Deletion for Post: {0}")]
    public async Task BroadcastCommentDeletionAsync(int postId, List<int> deletedCommentIds)
    {
        await _postsHubContext.Clients.Group($"post-updates_{postId}")
            .SendAsync("CommentsDeleted", deletedCommentIds); 
    }

    public async Task<ApiResponse<object>> DeletePostAsync(int postId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var post = await _unitOfWork.Posts.GetByIdAsync(postId);
        if (post == null || post.IsDeleted)
            return ApiResponse<object>.Ok(null, "Bài viết đã được xóa hoặc không tồn tại.");

        var userMembership = await _unitOfWork.GroupMembers.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(gm => gm.GroupID == post.GroupID && gm.UserID == userId);

        bool isAuthor = post.AuthorUserID == userId;
        bool isGroupAdminOrMod = userMembership != null && userMembership.Role > EnumGroupRole.Member;

        if (!isAuthor && !isGroupAdminOrMod)
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền xóa bài viết này.", 403);

        post.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue(() =>
            BroadcastPostDeletionAsync(post.GroupID, postId));

        if (post.AuthorUserID != userId)
        {
            var deleter = await _unitOfWork.Users.GetByIdAsync(userId);
            if (deleter != null)
            {
                var eventData = new PostDeletedByAdminEventData(post, deleter);
                _backgroundJobClient.Enqueue<INotificationService>(s =>
                    s.DispatchNotificationAsync<PostDeletedByAdminNotificationTemplate, PostDeletedByAdminEventData>(post.AuthorUserID, eventData));
            }
        }

        return ApiResponse<object>.Ok(null, "Xóa bài viết thành công.");
    }

    public async Task<ApiResponse<PostDetailDTO>> UpdatePostAsync(int postId, UpdatePostDto dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PostDetailDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var post = await _unitOfWork.Posts.GetByIdAsync(postId);
        if (post == null || post.IsDeleted)
            return ApiResponse<PostDetailDTO>.Fail("POST_NOT_FOUND", "Không tìm thấy bài viết.", 404);

        if (post.AuthorUserID != userId)
            return ApiResponse<PostDetailDTO>.Fail("FORBIDDEN", "Bạn không có quyền sửa bài viết này.", 403);

        post.Title = dto.Title;
        post.ContentJson = dto.ContentJson;
        post.ContentHtml = _contentRenderer.RenderAndSanitize(dto.ContentJson); 
        post.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        var updatedPostDto = _mapper.Map<PostDetailDTO>(post);

        _backgroundJobClient.Enqueue(() =>
            BroadcastPostUpdateAsync(postId, updatedPostDto));
        return ApiResponse<PostDetailDTO>.Ok(updatedPostDto, "Cập nhật bài viết thành công.");
    }

    public async Task<ApiResponse<object>> UpdatePinStatusAsync(int postId, UpdatePinStatusDto dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var post = await _unitOfWork.Posts.GetByIdAsync(postId);
        if (post == null || post.IsDeleted)
            return ApiResponse<object>.Fail("POST_NOT_FOUND", "Không tìm thấy bài viết.", 404);

        var userMembership = await _unitOfWork.GroupMembers.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(gm => gm.GroupID == post.GroupID && gm.UserID == userId);


        if (userMembership == null || userMembership.Role < EnumGroupRole.Moderator)
            return ApiResponse<object>.Fail("FORBIDDEN", "Chỉ Quản trị viên và Kiểm duyệt viên mới có quyền ghim bài viết.", 403);

        post.IsPinned = dto.IsPinned;
        await _unitOfWork.SaveChangesAsync();

        _backgroundJobClient.Enqueue(() =>
            BroadcastPostPinStatusAsync(post.GroupID, postId, dto.IsPinned));

        if (post.AuthorUserID != userId)
        {
            var pinner = await _unitOfWork.Users.GetByIdAsync(userId);
            if (pinner != null)
            {
                var eventData = new PostPinStatusChangedEventData(post, pinner, dto.IsPinned);
                _backgroundJobClient.Enqueue<INotificationService>(s =>
                    s.DispatchNotificationAsync<PostPinStatusChangedNotificationTemplate, PostPinStatusChangedEventData>(post.AuthorUserID, eventData));
            }
        }

        return ApiResponse<object>.Ok(null, dto.IsPinned ? "Ghim bài viết thành công." : "Bỏ ghim bài viết thành công.");
    }

    [DisplayName("Broadcast Post Update for Post: {0}")]
    public async Task BroadcastPostUpdateAsync(int postId, PostDetailDTO updatedPostDto)
    {
        await _postsHubContext.Clients.Group($"post-updates_{postId}")
            .SendAsync("PostUpdated", updatedPostDto);
    }

    [DisplayName("Broadcast Post Deletion for Group: {0}")]
    public async Task BroadcastPostDeletionAsync(Guid groupId, int postId)
    {
        await _postsHubContext.Clients.Group($"group-feed_{groupId}")
            .SendAsync("PostDeleted", postId);
        await _postsHubContext.Clients.Group($"post-updates_{postId}")
            .SendAsync("PostDeleted", postId);
    }

    [DisplayName("Broadcast Post Pin Status Change for Group: {0}")]
    public async Task BroadcastPostPinStatusAsync(Guid groupId, int postId, bool isPinned)
    {
        await _postsHubContext.Clients.Group($"group-feed_{groupId}")
            .SendAsync("PostPinStatusChanged", postId, isPinned);
        await _postsHubContext.Clients.Group($"post-updates_{postId}")
            .SendAsync("PostPinStatusChanged", postId, isPinned);
    }
}
