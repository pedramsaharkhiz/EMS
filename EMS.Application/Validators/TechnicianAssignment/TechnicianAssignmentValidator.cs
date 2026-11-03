using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.TechnicianAssignment;
using FluentValidation;

namespace EMS.Application.Validators.TechnicianAssignment
{


    public class TechnicianAssignmentValidator : AbstractValidator<TechnicianAssignmentDto>
    {
        public TechnicianAssignmentValidator()
        {
            RuleFor(x => x.FailureReportId).GreaterThan(0).WithMessage("شناسه اعلام خرابی 'تخصیص تکنسین' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.TechnicianAssignmentStatusId).GreaterThan(0).WithMessage("شناسه وضعیت تخصیص تکنسین 'تخصیص تکنسین' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.TechnicianUserId).GreaterThan(0).WithMessage("شناسه تکنسین 'تخصیص تکنسین' باید بزرگتر از صفر باشد.");
            //RuleFor(x => x.AssignedByUserId).GreaterThan(0);
            //RuleFor(x => x.AssignedDateTime).NotEmpty();
            RuleFor(x => x.DeadlineDateTime).GreaterThan(x => x.AssignedDateTime).When(x => x.DeadlineDateTime.HasValue).WithMessage("مهلت انجام کار 'تخصیص تکنسین' باید از تاریخ تحصیص بزرگتر باشد.");
            RuleFor(x => x.CompletedDateTime).GreaterThan(x => x.AssignedDateTime).When(x => x.CompletedDateTime.HasValue).WithMessage("تکمیل انجام کار 'تخصیص تکنسین' باید از تاریخ تحصیص بزرگتر باشد.");
            RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("توضیحات 'تخصیص تکنسین' باید حداکثر 500 کاراکتر باشد.");
            RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'تخصیص تکنسین' الزامی است.");;
        }
    }
}
