using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Domain.Enum;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class CreateChatGroupDtoValidator : AbstractValidator<CreateChatGroupDto>
{
    public CreateChatGroupDtoValidator()
    {
        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Tên nhóm không được để trống.")
            .MaximumLength(100).WithMessage("Tên nhóm không được vượt quá 100 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");

        // QUY TẮC PHỨC TẠP: Kiểm tra giá trị của Enum
        RuleFor(x => x.GroupType)
            .IsInEnum().WithMessage("Loại nhóm không hợp lệ.")
            .Must(type => type == EnumGroupType.Public || type == EnumGroupType.Private)
            .WithMessage("Loại nhóm cho nhóm chat chỉ có thể là 'Public' hoặc 'Private'.");

        RuleFor(x => x.InvitedUserIds)
            .Must(ids => ids == null || ids.Count < 50)
            .WithMessage("Không thể mời quá 50 thành viên cùng lúc khi tạo nhóm.");
    }
}
