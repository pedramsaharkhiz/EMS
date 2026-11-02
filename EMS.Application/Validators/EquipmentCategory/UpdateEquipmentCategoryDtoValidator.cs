using EMS.Application.Dtos.EquipmentCategory;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public class UpdateEquipmentCategoryDtoValidator : AbstractValidator<EquipmentCategoryUpdateDto>
    {
        public UpdateEquipmentCategoryDtoValidator()
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان دسته‌بندی نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.EquipmentCategoryId).GreaterThan(0).WithMessage("شناسه گروه بندی تجهیزات 'نوع تجهیزات' باید بزرگتر از صفر باشد.");

        }
    }
}
