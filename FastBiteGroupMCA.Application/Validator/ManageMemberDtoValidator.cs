using FastBiteGroupMCA.Application.DTOs.Group;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class ManageMemberDtoValidator : AbstractValidator<ManageMemberDTO>
{
    public ManageMemberDtoValidator()
    {
        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Hành động không hợp lệ.");
    }
}
