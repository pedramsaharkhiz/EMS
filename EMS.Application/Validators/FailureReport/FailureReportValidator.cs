using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.FailureReport;
using FluentValidation;

namespace EMS.Application.Validators.FailureReport
{
    public class FailureReportValidator : AbstractValidator<FailureReportDto>
    {
        public FailureReportValidator()
        {
            RuleFor(x => x.EquipmentId).GreaterThan(0).WithMessage("شناسه تجهیز 'اعلام خرابی' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.FailureTypeId).GreaterThan(0).WithMessage("شناسه نوع خرابی 'اعلام خرابی' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.ReportTypeId).GreaterThan(0).WithMessage("شناسه نوع گزارش خرابی 'اعلام خرابی' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.OrganizationId).GreaterThan(0).WithMessage("شناسه شعبه 'اعلام خرابی' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EmployeeId).GreaterThan(0).When(x => x.EmployeeId.HasValue).WithMessage("شناسه کارمند گزارش‌دهنده خرابی باید بزرگتر از صفر باشد.");
            RuleFor(x => x.PriorityId).InclusiveBetween(1, 5).WithMessage("اولویت 'اعلام خرابی' باید بین 1 تا 5 باشد."); // 1 = کم، 5 = فوری
            //RuleFor(x => x.ReportedDateTime).NotEmpty().WithMessage("تاریخ گزارش ‌شده 'اعلام خرابی' الزامی است.");
            RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("توضیحات 'اعلام خرابی' باید حداکثر 500 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'اعلام خرابی' الزامی است.");
            RuleFor(x => x.ReferenceNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.ReferenceNumber)).WithMessage("شماره ارجاع 'اعلام خرابی' باید حداکثر 50 کاراکتر باشد.");
        }
    }
}