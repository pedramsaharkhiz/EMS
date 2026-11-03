using EMS.Application.Dtos.EquipmentLocationType;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.EquipmentLocationType
{
    public class EquipmentLocationTypeValidator : AbstractValidator<EquipmentLocationTypeDto> 
    {
        public EquipmentLocationTypeValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'نوع محل تجهیزات' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'نوع محل تجهیزات' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'نوع محل تجهیزات' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'نوع محل تجهیزات' الزامی است.");
        }
    }
}
