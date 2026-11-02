using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentStatus;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentStatus
{
    public class UpdateEquipmentStatusDtoValidator : AbstractValidator<EquipmentStatusUpdateDto>
    {
        public UpdateEquipmentStatusDtoValidator()
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان وضعیت تجهیزات نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.EquipmentStatusId).GreaterThan(0).WithMessage("شناسه  'وضعیت تجهیزات' باید بزرگتر از صفر باشد.");

        }
    }
}
