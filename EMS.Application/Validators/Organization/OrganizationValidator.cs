using System;
using System.Collections.Generic;
using System.Text;
using EMS.Application.Dtos.Organization;
using FluentValidation;

namespace EMS.Application.Validators.Organization
{


    public class OrganizationValidator : AbstractValidator<OrganizationDto>
    {
        public OrganizationValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان 'شعبه' الزامی است.");
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان 'شعبه' باید حداکثر 100 کاراکتر باشد.");
            RuleFor(x => x.Code).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Code)).WithMessage("کد 'شعبه' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address)).WithMessage("آدرس 'شعبه' نمی‌تواند بیشتر از 500 کاراکتر باشد.");
            RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PhoneNumber)).WithMessage("شماره تلفن 'شعبه' نمی‌تواند بیشتر از 20 کاراکتر باشد.");
            RuleFor(x => x.OrganizationTypeId).GreaterThan(0).WithMessage("شناسه نوع شعبه 'شعبه' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.OrganizationManagementId).GreaterThan(0).When(x => x.OrganizationManagementId.HasValue).WithMessage("شناسه مدیریت شعب 'شعبه' باید بزرگتر از صفر باشد.");
            RuleFor(x => x.FieldId).GreaterThan(0).When(x => x.FieldId.HasValue).WithMessage("شناسه حوزه 'شعبه' باید بزرگتر از صفر باشد.");
            // Exactly one parent: a field (حوزه) or, without a field, a branch management (مدیریت شعب). The database enforces the same rule.
            RuleFor(x => x).Must(x => x.FieldId.HasValue != x.OrganizationManagementId.HasValue)
                .WithName("FieldId")
                .WithMessage("شعبه باید دقیقاً یکی از 'حوزه' یا 'مدیریت شعب' را داشته باشد (نه هیچ‌کدام و نه هر دو).");
            //RuleFor(x => x.IsActive).NotNull().WithMessage("وضعیت فعال بودن 'شعبه' الزامی است.");;
            //RuleFor(x => x.Latitude).NotNull();
            //RuleFor(x => x.Longitude).NotNull();
        }
    }
}
