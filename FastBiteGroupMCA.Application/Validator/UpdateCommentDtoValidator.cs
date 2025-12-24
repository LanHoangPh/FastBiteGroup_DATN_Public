using FastBiteGroupMCA.Application.DTOs.Comment;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung bình luận không được để trống.")
            .MaximumLength(2000).WithMessage("Bình luận không được vượt quá 2000 ký tự.");
    }
}
