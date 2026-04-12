using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.DTOs.Poll;
using FastBiteGroupMCA.Application.DTOs.Post;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using FastBiteGroupMCA.Infastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
namespace FastBiteGroupMCA.Infastructure.Services;

public class PollService : IPollService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMessagesRepository _messageRepo;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<PollService> _logger;
    private readonly IMapper _mapper;

    public PollService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IMessagesRepository messageRepo, IHubContext<ChatHub> hubContext, IMapper mapper, ILogger<PollService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _messageRepo = messageRepo;
        _hubContext = hubContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<object>> CastVoteAsync(int pollId, int pollOptionId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Options) 
            .FirstOrDefaultAsync(p => p.PollID == pollId);

        if (poll == null)
            return ApiResponse<object>.Fail("POLL_NOT_FOUND", "Không tìm thấy bình chọn.");

        if (!poll.Options.Any(o => o.PollOptionID == pollOptionId))
            return ApiResponse<object>.Fail("OPTION_NOT_FOUND", "Phương án lựa chọn không hợp lệ.");

        if (!await _unitOfWork.ConversationParticipants.GetQueryable().AnyAsync(p => p.ConversationID == poll.ConversationID && p.UserID == userId))
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền bỏ phiếu trong bình chọn này.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var userVotesInThisPoll = await _unitOfWork.PollVotes.GetQueryable()
                .Where(v => v.PollOption.PollID == pollId && v.UserID == userId)
                .ToListAsync();

            if (!poll.AllowMultipleChoices) 
            {
                _unitOfWork.PollVotes.RemoveRange(userVotesInThisPoll);
            }

            var existingVoteForThisOption = userVotesInThisPoll
                .FirstOrDefault(v => v.PollOptionID == pollOptionId);

            if (existingVoteForThisOption != null)
            {
                _unitOfWork.PollVotes.Remove(existingVoteForThisOption);
            }
            else
            {
                var newVote = new PollVotes { PollOptionID = pollOptionId, UserID = userId, VotedAt = DateTime.UtcNow };
                await _unitOfWork.PollVotes.AddAsync(newVote);
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi bỏ phiếu cho Poll {PollId} bởi người dùng {UserId}", pollId, userId);
            throw;
        }

        await BroadcastPollUpdates(pollId, poll.ConversationID);

        return ApiResponse<object>.Ok(null, "Bỏ phiếu thành công.");
    }

    private async Task BroadcastPollUpdates(int pollId, int conversationId)
    {
        var updatedPollState = await _unitOfWork.Polls.GetQueryable()
            .Where(p => p.PollID == pollId)
            .Select(p => new PollResultsDTO
            {
                PollId = p.PollID,
                Question = p.Question,
                AllowMultipleChoices = p.AllowMultipleChoices,
                Options = p.Options.Select(opt => new PollOptionResultDTO
                {
                    PollOptionId = opt.PollOptionID,
                    OptionText = opt.OptionText,
                    VoteCount = opt.Votes.Count(),
                    VotedByUsers = opt.Votes.Select(v => v.UserID).ToList()
                }).ToList()
                
            })
            .FirstOrDefaultAsync();

        if (updatedPollState != null)
        {
            await _hubContext.Clients.Group($"conversation_{conversationId}")
                             .SendAsync("PollUpdated", updatedPollState);
        }
    }

    public async Task<ApiResponse<CreatePollResponseDTO>> CreatePollAsync(int conversationId, CreatePollDTO dto)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<CreatePollResponseDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var sender = await _unitOfWork.ConversationParticipants.GetQueryable()
            .Where(p => p.ConversationID == conversationId && p.UserID == userId)
            .Select(p => p.User)
            .FirstOrDefaultAsync();

        if (sender == null)
            return ApiResponse<CreatePollResponseDTO>.Fail("FORBIDDEN", "Bạn không có quyền tạo bình chọn trong nhóm này.");
        var newPoll = new Polls
        {
            ConversationID = conversationId,
            CreatedByUserID = userId,
            Question = dto.Question,
            AllowMultipleChoices = dto.AllowMultipleChoices,
            CreatedAt = DateTime.UtcNow
        };

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Polls.AddAsync(newPoll);
            await _unitOfWork.SaveChangesAsync(); 

            var pollOptions = dto.Options.Select(opt => new PollOptions
            {
                PollID = newPoll.PollID,
                OptionText = opt
            }).ToList();

            await _unitOfWork.PollOptions.AddRangeAsync(pollOptions);
            await _unitOfWork.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi tạo bình chọn cho cuộc trò chuyện {ConversationId}", conversationId);
            throw;
        }

        var pollMessage = new Messages
        {
            ConversationId = conversationId,
            MessageType = EnumMessageType.Poll,
            SentAt = newPoll.CreatedAt,
            Sender = new SenderInfo { UserId = sender.Id, DisplayName = sender.FullName!, AvatarUrl = sender.AvatarUrl },
            Content = newPoll.PollID.ToString()
        };
        await _messageRepo.InsertOneAsync(pollMessage);

        var messageDto = _mapper.Map<MessageDTO>(pollMessage);
        await _hubContext.Clients.Group($"conversation_{conversationId}")
                         .SendAsync("ReceiveMessage", messageDto);

        var responseDto = new CreatePollResponseDTO
        {
            PollId = newPoll.PollID,
            PollMessage = messageDto 
        };

        return ApiResponse<CreatePollResponseDTO>.Ok(responseDto, "Tạo bình chọn thành công.");
    }

    public async Task<ApiResponse<PollDetailDTO>> GetPollDetailsAsync(int pollId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<PollDetailDTO>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var pollDetails = await _unitOfWork.Polls.GetQueryable()
            .Where(p => p.PollID == pollId && !p.IsDeleted)
            .Select(p => new PollDetailDTO 
            {
                PollId = p.PollID,
                Question = p.Question,
                ClosesAt = p.ClosesAt,
                TotalVoteCount = p.Options.SelectMany(o => o.Votes).Count(),
                Options = p.Options.Select(opt => new PollOptionDetailDTO
                {
                    OptionId = opt.PollOptionID,
                    OptionText = opt.OptionText,
                    VoteCount = opt.Votes.Count(),
                    Voters = opt.Votes.Select(v => new VoterDTO
                    {
                        UserId = v.UserID,
                        FullName = v.User!.FullName!,
                        AvatarUrl = v.User.AvatarUrl
                    }).ToList(),
                    HasVotedByCurrentUser = opt.Votes.Any(v => v.UserID == userId)
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (pollDetails == null)
            return ApiResponse<PollDetailDTO>.Fail("POLL_NOT_FOUND", "Không tìm thấy bình chọn.");

        return ApiResponse<PollDetailDTO>.Ok(pollDetails);
    }

    public async Task<ApiResponse<object>> ClosePollAsync(int pollId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var poll = await _unitOfWork.Polls.GetByIdAsync(pollId);
        if (poll == null)
            return ApiResponse<object>.Fail("POLL_NOT_FOUND", "Không tìm thấy bình chọn.");

        if (poll.CreatedByUserID != userId)
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền đóng bình chọn này.");

        if (poll.ClosesAt.HasValue)
            return ApiResponse<object>.Fail("ALREADY_CLOSED", "Bình chọn đã được đóng trước đó.");

        poll.ClosesAt = DateTime.UtcNow;
        _unitOfWork.Polls.Update(poll);
        await _unitOfWork.SaveChangesAsync();


        await BroadcastPollUpdates(pollId, poll.ConversationID);

        return ApiResponse<object>.Ok(null, "Đóng bình chọn thành công.");
    }

    public async Task<ApiResponse<object>> DeletePollAsync(int pollId)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return ApiResponse<object>.Fail("UNAUTHORIZED", "Không hợp lệ.");

        var poll = await _unitOfWork.Polls.GetByIdAsync(pollId);
        if (poll == null)
            return ApiResponse<object>.Fail("POLL_NOT_FOUND", "Không tìm thấy bình chọn.");


        var isCreator = poll.CreatedByUserID == userId;

        if (!isCreator)
            return ApiResponse<object>.Fail("FORBIDDEN", "Bạn không có quyền xóa bình chọn này.");

        poll.IsDeleted = true; 
        _unitOfWork.Polls.Update(poll);
        await _unitOfWork.SaveChangesAsync();


        await _hubContext.Clients.Group($"conversation_{poll.ConversationID}")
                         .SendAsync("PollDeleted", poll.PollID);

        return ApiResponse<object>.Ok(null, "Xóa bình chọn thành công.");
    }
}
