using FastBiteGroupMCA.Application.DTOs.Post;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class UpdatePostDtoValidator : AbstractValidator<UpdatePostDto>
{
    public UpdatePostDtoValidator()
    {
        RuleFor(x => x.ContentJson).NotEmpty().WithMessage("Nội dung không được để trống.");
    }
}
