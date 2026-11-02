using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentStatus;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentStatus
{
    public class CreateEquipmentStatusDtoValidator : AbstractValidator<EquipmentStatusCreateDto>
    {
        public CreateEquipmentStatusDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان وضعیت تجهیزات الزامی است.").MaximumLength(100).WithMessage("عنوان وضعیت تجهیزات نمی‌تواند بیشتر از 100 کاراکتر باشد.").Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان وضعیت تجهیزات نمی‌تواند فقط شامل فاصله باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'وضعیت تجهیزات' نمی‌تواند بیشتر از 20 کاراکتر باشد.");

        }
    }
}
