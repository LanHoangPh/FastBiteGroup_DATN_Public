using FastBiteGroupMCA.Application.DTOs.User;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDTO>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.");
        // FluentValidation có các quy tắc phức tạp hơn như .Matches("[A-Z]") v.v. nếu bạn cần

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Vui lòng xác nhận mật khẩu mới.")
            .Equal(x => x.NewPassword).WithMessage("Mật khẩu mới và mật khẩu xác nhận không trùng khớp.");
    }
}
