using EMS.Application.Dtos.FailureCategory;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.FailureCategory
{
    public class FailureCategoryValidator : AbstractValidator<FailureCategoryDto> 
    {
        public FailureCategoryValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'گروه بندی خرابی' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'گروه بندی خرابی' باید حداکثر 100 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'گروه بندی خرابی' الزامی است.");
        }
    }

    
}
