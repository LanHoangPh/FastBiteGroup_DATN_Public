using FastBiteGroupMCA.Application.DTOs.Message;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class SendMessageDtoValidator : AbstractValidator<SendMessageDTO>
{
    public SendMessageDtoValidator()
    {
        RuleFor(x => x.Content)
            .MaximumLength(2000).WithMessage("Nội dung tin nhắn không được vượt quá 2000 ký tự.");
        RuleFor(x => x.ParentMessageId)
            .Must(BeAValidParentMessageId).WithMessage("ID tin nhắn cha không hợp lệ.");
        RuleFor(x => x)
            .Must(dto => !string.IsNullOrWhiteSpace(dto.Content) || (dto.AttachmentFileIds != null && dto.AttachmentFileIds.Any()))
            .WithMessage("Tin nhắn phải có nội dung hoặc tệp đính kèm.");
    }
    private bool BeAValidParentMessageId(string? parentMessageId)
    {
        if (string.IsNullOrEmpty(parentMessageId)) return true;
        return !string.IsNullOrWhiteSpace(parentMessageId);
    }
}
