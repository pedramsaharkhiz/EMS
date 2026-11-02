using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.SupportingDepartment;
using FluentValidation;

namespace EMS.Application.Validators.SupportingDepartment
{
    public class UpdateSupportingDepartmentDtoValidator : AbstractValidator<SupportingDepartmentUpdateDto>
    {
        public UpdateSupportingDepartmentDtoValidator()
        {
            RuleFor(x => x.Title).MaximumLength(100).WithMessage("عنوان دپارتمان پشتیبانی نمی‌تواند بیشتر از 100 کاراکتر باشد.");
            RuleFor(x => x.CityId).GreaterThan(0).When(x => x.CityId.HasValue).WithMessage("شناسه شهر باید بزرگتر از صفر باشد.");
            RuleFor(x => x.SupportingDepartmentId).GreaterThan(0).WithMessage("شناسه  'دپارتمان پشتیبانی' باید بزرگتر از صفر باشد.");

        }
    }
}