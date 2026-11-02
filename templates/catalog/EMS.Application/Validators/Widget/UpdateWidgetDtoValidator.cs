using EMS.Application.Dtos.Widget;
using FluentValidation;

namespace EMS.Application.Validators.Widget
{
    public class UpdateWidgetDtoValidator : AbstractValidator<WidgetUpdateDto>
    {
        public UpdateWidgetDtoValidator()
        {
            RuleFor(x => x.WidgetId).GreaterThan(0).WithMessage("شناسه __LABEL__ باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان __LABEL__ نمی‌تواند بیشتر از 100 کاراکتر باشد.");
//#if (HasCode)
            RuleFor(x => x.Code)
                .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code))
                .WithMessage("کد __LABEL__ نمی‌تواند بیشتر از 20 کاراکتر باشد.");
//#endif
        }
    }
}
