using EMS.Application.Dtos.FailureCategory;
using FluentValidation;

namespace EMS.Application.Validators.FailureCategory
{
    public class CreateFailureCategoryDtoValidator : AbstractValidator<FailureCategoryCreateDto>
    {
        public CreateFailureCategoryDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان دسته‌بندی الزامی است.").MaximumLength(100).WithMessage("عنوان دسته‌بندی نمی‌تواند بیشتر از 100 کاراکتر باشد.").Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان دسته‌بندی نمی‌تواند فقط شامل فاصله باشد.");
        }
    }
}
