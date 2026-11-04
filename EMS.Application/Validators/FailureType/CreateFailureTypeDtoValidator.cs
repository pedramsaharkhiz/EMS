using EMS.Application.Dtos.FailureType;
using EMS.Application.Services.FailureType;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.FailureType
{
    // Title (required, max length) and Code (max length) rules come from the base class.
    // Add rules for parents etc. in the constructor, e.g. RuleFor(x => x.CategoryId).GreaterThan(0)...
    public sealed class CreateFailureTypeDtoValidator : CatalogCreateValidator<FailureTypeCreateDto>
    {
        public CreateFailureTypeDtoValidator() : base(FailureTypeService.EntityLabel, x => x.Code)
        {
            RuleFor(x => x.FailureCategoryId).GreaterThan(0).WithMessage("شناسه دسته‌ی خرابی باید بزرگتر از صفر باشد.");
        }
    }
}
