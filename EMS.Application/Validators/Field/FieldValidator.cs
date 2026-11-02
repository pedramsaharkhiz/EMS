using EMS.Application.Dtos.Field;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.Field
{
    public class FieldValidator : AbstractValidator<FieldDto> 
    {
        public FieldValidator()
        {
            RuleFor(x => x.OrganizationManagementId).GreaterThan(0).WithMessage("شناسه مدیریت شعب 'ناحیه' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'ناحیه' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'ناحیه' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'ناحیه' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'ناحیه' الزامی است.");;

        }
    }
}
