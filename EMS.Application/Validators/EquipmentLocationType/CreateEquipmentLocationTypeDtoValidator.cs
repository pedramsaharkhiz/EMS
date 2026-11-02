using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentLocationType;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentLocationType
{
    public class CreateEquipmentLocationTypeDtoValidator : AbstractValidator<EquipmentLocationTypeCreateDto>
    {
        public CreateEquipmentLocationTypeDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان نوع محل تجهیزات الزامی است.").MaximumLength(100).WithMessage("عنوان نوع محل تجهیزات نمی‌تواند بیشتر از 100 کاراکتر باشد.").Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان نوع محل تجهیزات نمی‌تواند فقط شامل فاصله باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'نوع محل تجهیزات' نمی‌تواند بیشتر از 20 کاراکتر باشد.");

        }
    }
}
