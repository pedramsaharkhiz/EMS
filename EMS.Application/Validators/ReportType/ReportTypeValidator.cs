using EMS.Application.Dtos.ReportType;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.ReportType
{
    public class ReportTypeValidator : AbstractValidator<ReportTypeDto> 
    {
        public ReportTypeValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'نوع گزارش خرابی' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'نوع گزارش خرابی' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'نوع گزارش خرابی' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'نوع گزارش خرابی' الزامی است.");;

        }
    }
}
