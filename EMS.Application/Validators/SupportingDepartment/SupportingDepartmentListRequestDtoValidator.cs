using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.SupportingDepartment;
using FluentValidation;

namespace EMS.Application.Validators.SupportingDepartment
{
    public class SupportingDepartmentListRequestDtoValidator : AbstractValidator<SupportingDepartmentListRequestDto>
    {
        public SupportingDepartmentListRequestDtoValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل 1 باشد.");
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage(" بین 1 تا 100 باشد.");
            RuleFor(x => x.CityId).GreaterThan(0).When(x => x.CityId.HasValue).WithMessage("شناسه شهر باید بزرگتر از صفر باشد.");
            RuleFor(x => x.SortDirection).Must(x => string.IsNullOrWhiteSpace(x) || x.Equals("asc", StringComparison.OrdinalIgnoreCase) || x.Equals("desc", StringComparison.OrdinalIgnoreCase)).WithMessage("جهت مرتب‌سازی باید asc یا desc باشد.");
            //RuleFor(x => x.Search).MaximumLength(100).WithMessage("عبارت جستجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
        }
    }
}