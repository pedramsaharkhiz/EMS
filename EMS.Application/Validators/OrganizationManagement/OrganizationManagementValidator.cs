using EMS.Application.Dtos.OrganizationManagement;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.OrganizationManagement
{
    public class OrganizationManagementValidator : AbstractValidator<OrganizationManagementDto> 
    { 
        public OrganizationManagementValidator() 
        {

            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'مدیریت شعب' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'مدیریت شعب' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'مدیریت شعب' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            RuleFor(x => x.ZoneId).GreaterThan(0).WithMessage("شناسه حوزه 'مدیریت شعب' باید بزرگتر از صفر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'مدیریت شعب' الزامی است.");; 
        } 
    }
}
