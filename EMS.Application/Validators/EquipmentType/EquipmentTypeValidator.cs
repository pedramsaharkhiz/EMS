using EMS.Application.Dtos.EquipmentType;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.EquipmentType
{
    public class EquipmentTypeValidator : AbstractValidator<EquipmentTypeDto> 
    { 
        public EquipmentTypeValidator() 
        { 
            RuleFor(x => x.EquipmentCategoryId).GreaterThan(0).WithMessage("شناسه گروه بندی تجهیزات 'نوع تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'نوع تجهیزات' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'نوع تجهیزات' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'نوع تجهیزات' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'نوع تجهیزات' الزامی است.");
        } 
    }
}
