using EMS.Application.Dtos.FailureType;
using EMS.Application.Services.FailureType;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.FailureType
{
    // Title (max length) and Code (max length) rules come from the base class (the id comes from the route).
    public sealed class UpdateFailureTypeDtoValidator : CatalogUpdateValidator<FailureTypeUpdateDto>
    {
        public UpdateFailureTypeDtoValidator() : base(FailureTypeService.EntityLabel, x => x.Code)
        {
            RuleFor(x => x.FailureCategoryId).GreaterThan(0).WithMessage("شناسه دسته‌ی خرابی باید بزرگتر از صفر باشد.");
            RuleFor(x => x.EquipmentTypeIds).Must(ids => ids is null || ids.Count <= 200).WithMessage("یک نوع خرابی نمی‌تواند به بیشتر از ۲۰۰ نوع تجهیز وصل شود.");
            RuleForEach(x => x.EquipmentTypeIds).GreaterThan(0).WithMessage("شناسه‌ی نوع تجهیز باید بزرگتر از صفر باشد.");
        }
    }
}
