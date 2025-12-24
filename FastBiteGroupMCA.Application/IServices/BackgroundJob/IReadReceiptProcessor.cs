using FastBiteGroupMCA.Application.DTOs.Message;

namespace FastBiteGroupMCA.Application.IServices.BackgroundJob;

public interface IReadReceiptProcessor
{
    Task ProcessAsync(MarkAsReadDTO dto, Guid readerUserId);
}
