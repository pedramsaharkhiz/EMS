using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentLocationType;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentLocationType
{
    public class UpdateEquipmentLocationTypeDtoValidator : AbstractValidator<EquipmentLocationTypeUpdateDto>
    {
        public UpdateEquipmentLocationTypeDtoValidator() 
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان نوع محل تجهیزات نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.EquipmentLocationTypeId).GreaterThan(0).WithMessage("شناسه  'نوع محل تجهیزات' باید بزرگتر از صفر باشد.");

        }
    }
}
