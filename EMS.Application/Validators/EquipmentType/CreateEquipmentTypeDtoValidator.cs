using EMS.Application.Dtos.EquipmentCategory;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public class CreateEquipmentTypeDtoValidator : AbstractValidator<EquipmentTypeCreateDto>
    {
        public CreateEquipmentTypeDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان نوع تجهیزات الزامی است.").MaximumLength(100).WithMessage("عنوان نوع تجهیزات نمی‌تواند بیشتر از 100 کاراکتر باشد.").Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان نوع تجهیزات نمی‌تواند فقط شامل فاصله باشد.");
            RuleFor(x => x.EquipmentCategoryId).GreaterThan(0).WithMessage("شناسه گروه بندی تجهیزات 'نوع تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'نوع تجهیزات' نمی‌تواند بیشتر از 20 کاراکتر باشد.");

        }
    }
}
