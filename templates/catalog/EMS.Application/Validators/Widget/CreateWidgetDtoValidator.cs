using EMS.Application.Dtos.Widget;
using FluentValidation;

namespace EMS.Application.Validators.Widget
{
    public class CreateWidgetDtoValidator : AbstractValidator<WidgetCreateDto>
    {
        public CreateWidgetDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان __LABEL__ الزامی است.")
                .MaximumLength(100).WithMessage("عنوان __LABEL__ نمی‌تواند بیشتر از 100 کاراکتر باشد.")
                .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان __LABEL__ نمی‌تواند فقط شامل فاصله باشد.");
//#if (HasCode)
            RuleFor(x => x.Code)
                .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code))
                .WithMessage("کد __LABEL__ نمی‌تواند بیشتر از 20 کاراکتر باشد.");
//#endif
        }
    }
}
