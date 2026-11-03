using EMS.Application.Dtos.FailureType;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.FailureType
{
    public class FailureTypeValidator : AbstractValidator<FailureTypeDto> 
    { 
        public FailureTypeValidator() 
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'نوع خرابی' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'نوع خرابی' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'نوع خرابی' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            RuleFor(x => x.FailureCategoryId).GreaterThan(0).When(x => x.FailureCategoryId.HasValue).WithMessage("شناسه گروه بندی خرابی 'نوع خرابی' باید بزرگتر از صفر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'نوع خرابی' الزامی است.");;
        } 
    }
}
