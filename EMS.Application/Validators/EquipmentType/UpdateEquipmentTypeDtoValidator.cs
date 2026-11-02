using EMS.Application.Dtos.EquipmentType;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public class UpdateEquipmentTypeDtoValidator : AbstractValidator<EquipmentTypeUpdateDto>
    {
        public UpdateEquipmentTypeDtoValidator()
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان نوع تجهیزات نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.EquipmentCategoryId).GreaterThan(0).WithMessage("شناسه گروه بندی تجهیزات 'نوع تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EquipmentTypeId).GreaterThan(0).WithMessage("شناسه  'نوع تجهیزات' باید بزرگتر از صفر باشد.");

        }
    }
}
