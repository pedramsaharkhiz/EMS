using EMS.Application.Dtos.FailureCategory;
using FluentValidation;

namespace EMS.Application.Validators.FailureCategory
{
    public class UpdateFailureCategoryDtoValidator : AbstractValidator<FailureCategoryUpdateDto>
    {
        public UpdateFailureCategoryDtoValidator()
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان دسته‌بندی نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.FailureCategoryId).GreaterThan(0).WithMessage("شناسه گروه بندی خرابی 'نوع خرابی' باید بزرگتر از صفر باشد.");

        }
    }
}
