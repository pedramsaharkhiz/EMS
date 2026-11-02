using EMS.Application.Dtos.EquipmentType;
using FluentValidation;

namespace EMS.Application.Validators.EquipmentCategory
{
    public class EquipmentTypeListRequestDtoValidator : AbstractValidator<EquipmentTypeListRequestDto>
    {
        public EquipmentTypeListRequestDtoValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل 1 باشد.");
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage(" بین 1 تا 100 باشد.");
            RuleFor(x => x.SortDirection).Must(x => string.IsNullOrWhiteSpace(x) || x.Equals("asc", StringComparison.OrdinalIgnoreCase) || x.Equals("desc", StringComparison.OrdinalIgnoreCase)).WithMessage("جهت مرتب‌سازی باید asc یا desc باشد.");
            //RuleFor(x => x.Search).MaximumLength(100).WithMessage("عبارت جستجو نمی‌تواند بیشتر از 100 کاراکتر باشد.");
        }
    }
}
