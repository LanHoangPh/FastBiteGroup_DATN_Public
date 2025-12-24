using FastBiteGroupMCA.Application.DTOs.ContentReport;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class CreateContentReportDtoValidator : AbstractValidator<CreateContentReportDto>
{
    public CreateContentReportDtoValidator()
    {
        RuleFor(x => x.ContentId)
            .GreaterThan(0).WithMessage("ID của nội dung không hợp lệ.");

        RuleFor(x => x.ContentType)
            .IsInEnum().WithMessage("Loại nội dung không hợp lệ.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do báo cáo không được để trống.")
            .MaximumLength(500).WithMessage("Lý do không được vượt quá 500 ký tự.");
    }
}
