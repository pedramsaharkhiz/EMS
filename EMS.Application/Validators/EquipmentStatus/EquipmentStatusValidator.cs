using EMS.Application.Dtos.EquipmentStatus;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.EquipmentStatus
{
    public class EquipmentStatusValidator : AbstractValidator<EquipmentStatusDto> 
    {
        public EquipmentStatusValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'وضعیت تجهیزات' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'وضعیت تجهیزات' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'وضعیت تجهیزات' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'وضعیت تجهیزات' الزامی است.");
        }
    }
}
