using FastBiteGroupMCA.Application.DTOs.Auth;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class RegisterDtoValidate : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidate()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}
