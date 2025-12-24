using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Domain.Enum;
using FluentValidation;

namespace FastBiteGroupMCA.Application.Validator;

public class GetPublicGroupsQueryValidator : AbstractValidator<GetPublicGroupsQuery>
{
    public GetPublicGroupsQueryValidator()
    {
        // Rule cho các tham số phân trang cơ bản
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Kích thước trang phải lớn hơn hoặc bằng 1.")
            .LessThanOrEqualTo(100).WithMessage("Kích thước trang không được vượt quá 100.");

        // --- QUY TẮC VALIDATE QUAN TRỌNG MÀ BẠN ĐÃ ĐỀ XUẤT ---
        RuleFor(x => x.FilterType)
            .IsInEnum().WithMessage("Loại bộ lọc không hợp lệ.")
            .When(x => x.FilterType.HasValue); // Chỉ kiểm tra khi có giá trị
    }
}
