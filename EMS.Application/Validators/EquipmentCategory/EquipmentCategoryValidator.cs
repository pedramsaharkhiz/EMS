using EMS.Application.Dtos.EquipmentCategory;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.EquipmentCategory
{
    public class EquipmentCategoryValidator : AbstractValidator<EquipmentCategoryDto> 
    {
        public EquipmentCategoryValidator() 
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'گروه بندی تجهیزات' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'گروه بندی تجهیزات' باید حداکثر 100 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'گروه بندی تجهیزات' الزامی است.");
        }
    }
}
