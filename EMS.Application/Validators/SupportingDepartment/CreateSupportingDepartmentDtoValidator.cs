using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.SupportingDepartment;
using FluentValidation;

namespace EMS.Application.Validators.SupportingDepartment
{
    public class CreateSupportingDepartmentDtoValidator : AbstractValidator<SupportingDepartmentCreateDto>
    {
        public CreateSupportingDepartmentDtoValidator()
        {
            RuleFor(x => x.CityId).GreaterThan(0).WithMessage("شناسه شهر باید بزرگتر از صفر باشد.");
            RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان دپارتمان پشتیبانی الزامی است.").MaximumLength(100).WithMessage("عنوان دپارتمان پشتیبانی نمی‌تواند بیشتر از 100 کاراکتر باشد.").Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("عنوان دپارتمان پشتیبانی نمی‌تواند فقط شامل فاصله باشد.");

        }
    }
}