using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.Equipment;
using FluentValidation;

namespace EMS.Application.Validators.Equipment
{
    public class EquipmentValidator : AbstractValidator<EquipmentDto>
    {
        public EquipmentValidator()
        {
            RuleFor(x => x.SerialNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.SerialNumber)).WithMessage("سریال نامبر 'تجهیزات' باید حداکثر 50 کاراکتر باشد.");
            RuleFor(x => x.AssetNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.AssetNumber)).WithMessage("شماره دارایی 'تجهیزات' باید حداکثر 50 کاراکتر باشد.");
            RuleFor(x => x.YaasCode).MaximumLength(50).WithMessage("یاس کد 'تجهیزات' باید حداکثر 50 کاراکتر باشد."); ;
            RuleFor(x => x.YaasCode).NotEmpty().WithMessage("یاس کد 'تجهیزات' الزامی است."); ;
            //RuleFor(x => x.EquipmentModelId).GreaterThan(0).WithMessage("شناسه مدل تجهیز 'تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EquipmentStatusId).GreaterThan(0).WithMessage("شناسه وضعیت تجهیز 'تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.CurrentEquipmentLocationTypeId).GreaterThan(0).WithMessage("شناسه نوع محل تجهیز 'تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.CurrentOrganizationId).GreaterThan(0).When(x => x.CurrentOrganizationId.HasValue).WithMessage("شناسه شعبه ' تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.CurrentSupportingDepartmentId).GreaterThan(0).When(x => x.CurrentSupportingDepartmentId.HasValue).WithMessage("شناسه دپارتمان پشتیبانی ' تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.TechnicianUserId).GreaterThan(0).When(x => x.TechnicianUserId.HasValue).WithMessage("شناسه تکنسین 'تجهیزات' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("توضیحات 'تجهیزات' باید حداکثر 500 کاراکتر باشد.");

            RuleFor(x => x)
            .Must(x =>
                (x.EquipmentTypeId.HasValue && !x.EquipmentModelId.HasValue) ||
                (!x.EquipmentTypeId.HasValue && x.EquipmentModelId.HasValue))
            .WithMessage("دقیقاً یکی از فیلدهای EquipmentTypeId یا EquipmentModelId باید مقدار داشته باشد.");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'تجهیزات' الزامی است.");
        }
    }
}
