using EMS.Application.Dtos.EquipmentModel;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.EquipmentModel
{
    public class EquipmentModelValidator : AbstractValidator<EquipmentModelDto> 
    { 
        public EquipmentModelValidator() 
        {
            RuleFor(x => x.EquipmentTypeId).GreaterThan(0).WithMessage("شناسه تجهیزات 'مدل تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("شناسه برند 'مدل تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'مدل تجهیزات' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'مدل تجهیزات' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'مدل تجهیزات' باید حداکثر 20 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'مدل تجهیزات' الزامی است.");
        } 
    }
}
