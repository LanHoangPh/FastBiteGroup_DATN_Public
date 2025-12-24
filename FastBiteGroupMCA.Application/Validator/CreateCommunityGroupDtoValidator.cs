using FastBiteGroupMCA.Application.DTOs.Group;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class CreateCommunityGroupDtoValidator : AbstractValidator<CreateCommunityGroupDto>
{
    public CreateCommunityGroupDtoValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Tên cộng đồng không được để trống.")
            .MaximumLength(100).WithMessage("Tên cộng đồng không được vượt quá 100 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");

        RuleFor(x => x.Privacy)
            .IsInEnum().WithMessage("Quyền riêng tư không hợp lệ.");
    }
}
