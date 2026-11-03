using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.TechnicianAssignmentAction;
using FluentValidation;

namespace EMS.Application.Validators.TechnicianAssignmentAction
{
    public class TechnicianAssignmentActionValidator : AbstractValidator<TechnicianAssignmentActionDto>
    {
        public TechnicianAssignmentActionValidator()
        {
            RuleFor(x => x.TechnicianAssignmentId).GreaterThan(0).WithMessage("شناسه تخصیص تکنسین 'TechnicianAssignmentAction' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.ActionId).GreaterThan(0).WithMessage("شناسه اکشن 'TechnicianAssignmentAction' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EquipmentLocationHistoryId).GreaterThan(0).WithMessage("شناسه تاریخچه محل تجهیز 'TechnicianAssignmentAction' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.ActionDateTime).NotEmpty().WithMessage("تاریخ و زمان اقدام 'TechnicianAssignmentAction' الزامی است.");
            RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("توضیحات 'TechnicianAssignmentAction' باید حداکثر 500 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'TechnicianAssignmentAction' الزامی است.");;
        }
    }
}
