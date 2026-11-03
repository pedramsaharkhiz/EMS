using EMS.Application.Dtos.TechnicianAssignmentStatus;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Validators.TechnicianAssignmentStatus
{
    public class TechnicianAssignmentStatusValidator : AbstractValidator<TechnicianAssignmentStatusDto> 
    {
        public TechnicianAssignmentStatusValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'وضعیت تخصیص تکنسین' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'وضعیت تخصیص تکنسین' باید حداکثر 100 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'وضعیت تخصیص تکنسین' الزامی است.");;
        }
    }
}
