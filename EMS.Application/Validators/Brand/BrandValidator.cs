using EMS.Application.Dtos.Brand;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.Brand
{
    public class BrandValidator : AbstractValidator<BrandDto> 
    { 
        public BrandValidator() 
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'برند' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'برند' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'برند' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'برند' الزامی است.");
        } 
    }
}
