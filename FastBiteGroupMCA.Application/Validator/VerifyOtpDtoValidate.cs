using FastBiteGroupMCA.Application.DTOs.Auth;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator
{
    public class VerifyOtpDtoValidate : AbstractValidator<VerifyOtpDto>
    {
        public VerifyOtpDtoValidate()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("OTP is required.")
                .Length(6).WithMessage("OTP must be exactly 6 digits.")
                .Matches(@"^\d{6}$").WithMessage("OTP must consist of only digits.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }
}
