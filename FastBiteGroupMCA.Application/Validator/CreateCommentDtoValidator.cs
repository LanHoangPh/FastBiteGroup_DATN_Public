using FastBiteGroupMCA.Application.DTOs.Comment;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDTO>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung bình luận không được để trống.")
            .MaximumLength(2000).WithMessage("Nội dung bình luận không được vượt quá 2000 ký tự.");

        RuleFor(x => x.ParentCommentId)
            .GreaterThan(0).When(x => x.ParentCommentId.HasValue)
            .WithMessage("ID bình luận cha không hợp lệ.");
    }
}

