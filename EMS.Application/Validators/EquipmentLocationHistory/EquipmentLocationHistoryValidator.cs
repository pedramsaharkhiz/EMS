using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.EquipmentLocationHistory;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentLocationHistory
{
    public class EquipmentLocationHistoryValidator : AbstractValidator<EquipmentLocationHistoryDto>
    {
        public EquipmentLocationHistoryValidator()
        {
            RuleFor(x => x.EquipmentId).GreaterThan(0).WithMessage("شناسه تجهیزات 'تاریخچه محل تجهیزات' باید بزرگتر از صفر باشد."); 
            RuleFor(x => x.EquipmentLocationTypeId).GreaterThan(0).WithMessage("شناسه نوع مجل تجهیزات 'تاریخچه محل تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.OrganizationId).GreaterThan(0).When(x => x.OrganizationId.HasValue).WithMessage("شناسه شعبه 'تاریخچه محل تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.SupportingDepartmentId).GreaterThan(0).When(x => x.SupportingDepartmentId.HasValue).WithMessage("شناسه دپارتمان پشتیبانی 'تاریخچه محل تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.TechnicianUserId).GreaterThan(0).When(x => x.TechnicianUserId.HasValue).WithMessage("شناسه تکنسین 'تاریخچه محل تجهیزات' باید بزرگتر از صفر باشد.");
            //RuleFor(x => x.AssignedDateTime).NotEmpty().WithMessage("تاریخ و زمان اختصاص داده شده 'تاریخچه محل تجهیزات' الزامی است.");
            //RuleFor(x => x.ChangedByUserId).GreaterThan(0).When(x => x.ChangedByUserId.HasValue);
            RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("توضیحات 'تاریخچه محل تجهیزات' باید حداکثر 500 کاراکتر باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'تاریخچه محل تجهیزات' الزامی است.");
        }
    }
}
