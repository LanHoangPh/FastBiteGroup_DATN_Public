using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Domain.Abstractions.Repository;
using MongoDB.Driver;

namespace FastBiteGroupMCA.Infastructure.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagesRepository _messagesRepo;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    public ChatService(IUnitOfWork unitOfWork, IMessagesRepository messagesRepo, ICurrentUser currentUser, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _messagesRepo = messagesRepo;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    //public async Task<ApiResponse<ToggleReactionResponseDTO>> ToggleReactionAsync(ToggleReactionDTO dto, Guid userId)
    //{
    //    var message = await _messagesRepo.GetByIdAsync(dto.MessageId); // Vẫn cần gọi lần đầu để lấy ConversationId
    //    if (message == null)
    //        return ApiResponse<ToggleReactionResponseDTO>.Fail("MessageNotFound", "Không tìm thấy tin nhắn.");

    //    if (!await _unitOfWork.ConversationParticipants.GetQueryable()
    //        .AnyAsync(p => p.ConversationID == message.ConversationId && p.UserID == userId))
    //        return ApiResponse<ToggleReactionResponseDTO>.Fail("Forbidden", "Bạn không có quyền tương tác.");

    //    var existingReaction = message.Reactions?.FirstOrDefault(r => r.UserId == userId && r.ReactionCode == dto.ReactionCode);
    //    var updateFilter = Builders<Messages>.Filter.Eq(m => m.Id, dto.MessageId);
    //    UpdateDefinition<Messages> updateAction;

    //    if (existingReaction != null)
    //    {
    //        updateAction = Builders<Messages>.Update.PullFilter(m => m.Reactions, r => r.UserId == userId && r.ReactionCode == dto.ReactionCode);
    //    }
    //    else
    //    {
    //        var newReaction = new Reaction { UserId = userId, ReactionCode = dto.ReactionCode, ReactedAt = DateTime.UtcNow };
    //        updateAction = Builders<Messages>.Update.AddToSet(m => m.Reactions, newReaction);
    //    }

    //    // --- THAY ĐỔI QUAN TRỌNG ---
    //    // Dùng FindOneAndUpdate để vừa cập nhật vừa lấy kết quả mới trong 1 lệnh
    //    var updatedMessage = await _messagesRepo.FindOneAndUpdateAsync(updateFilter, updateAction);

    //    var responseDto = new ToggleReactionResponseDTO
    //    {
    //        NewReactions = updatedMessage?.Reactions ?? new List<Reaction>(),
    //        ConversationId = message.ConversationId
    //    };

    //    return ApiResponse<ToggleReactionResponseDTO>.Ok(responseDto);
    //}
}
