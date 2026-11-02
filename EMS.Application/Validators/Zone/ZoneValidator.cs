using EMS.Application.Dtos.Zone;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.Zone
{
    public class ZoneValidator : AbstractValidator<ZoneDto> 
    {
        public ZoneValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'حوزه' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'حوزه' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'حوزه' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
        }
        
    }
}
