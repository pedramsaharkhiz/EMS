using EMS.Application.Dtos.OrganizationType;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.OrganizationType
{
    public class OrganizationTypeValidator : AbstractValidator<OrganizationTypeDto> 
    {
        public OrganizationTypeValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'نوع شعبه' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'نوع شعبه' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'نوع شعبه' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'نوع شعبه' الزامی است.");;
        }
    }
}
