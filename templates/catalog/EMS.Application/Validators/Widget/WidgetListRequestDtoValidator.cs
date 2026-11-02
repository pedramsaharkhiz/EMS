using EMS.Application.Dtos.Widget;
using FluentValidation;

namespace EMS.Application.Validators.Widget
{
    public class WidgetListRequestDtoValidator : AbstractValidator<WidgetListRequestDto>
    {
        public WidgetListRequestDtoValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل 1 باشد.");
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("اندازه صفحه باید بین 1 تا 100 باشد.");
            RuleFor(x => x.SortDirection)
                .Must(x => string.IsNullOrWhiteSpace(x) || x.Equals("asc", StringComparison.OrdinalIgnoreCase) || x.Equals("desc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("جهت مرتب‌سازی باید asc یا desc باشد.");
        }
    }
}
