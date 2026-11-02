using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentModel;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentModel
{
    public class UpdateEquipmentModelDtoValidator : AbstractValidator<EquipmentModelUpdateDto>
    {
        public UpdateEquipmentModelDtoValidator()
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان مدل تجهیزات نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.EquipmentTypeId).GreaterThan(0).WithMessage("شناسه نوع تجهیزات 'مدل تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("شناسه برند 'مدل تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EquipmentModelId).GreaterThan(0).WithMessage("شناسه  'مدل تجهیزات' باید بزرگتر از صفر باشد.");

        }
    }
}
