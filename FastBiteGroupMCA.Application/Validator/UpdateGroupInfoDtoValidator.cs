using FastBiteGroupMCA.Application.DTOs.Group;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class UpdateGroupInfoDtoValidator : AbstractValidator<UpdateGroupInfoDto>
{
    public UpdateGroupInfoDtoValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Tên nhóm không được để trống.")
            .MaximumLength(100).WithMessage("Tên nhóm không được vượt quá 100 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");

        RuleFor(x => x.Privacy).IsInEnum().WithMessage("Quyền riêng tư không hợp lệ.");
    }
}
