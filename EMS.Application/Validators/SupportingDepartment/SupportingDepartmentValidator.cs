using EMS.Application.Dtos.SupportingDepartment;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.SupportingDepartment
{
    public class SupportingDepartmentValidator : AbstractValidator<SupportingDepartmentDto>
    {
        public SupportingDepartmentValidator()
        {
            RuleFor(x => x.CityId).GreaterThan(0).WithMessage("شناسه شهر باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'دپارتمان پشتیبانی' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'دپارتمان پشتیبانی' باید حداکثر 100 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'دپارتمان پشتیبانی' الزامی است.");; 
        }
    }
}