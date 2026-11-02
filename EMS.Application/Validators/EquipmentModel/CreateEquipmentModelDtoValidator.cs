using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentModel;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentModel
{
    public class CreateEquipmentModelDtoValidator : AbstractValidator<EquipmentModelCreateDto>
    {
        public CreateEquipmentModelDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان مدل تجهیزات الزامی است.").MaximumLength(100).WithMessage("عنوان مدل تجهیزات نمی‌تواند بیشتر از 100 کاراکتر باشد.").Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان مدل تجهیزات نمی‌تواند فقط شامل فاصله باشد.");
            RuleFor(x => x.EquipmentTypeId).GreaterThan(0).WithMessage("شناسه نوع تجهیزات 'مدل تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("شناسه برند 'مدل تجهیزات' باید بزرگتر از صفر باشد.");
        }
    }
}
