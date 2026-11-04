using EMS.Application.Dtos.ActionEntity;
using EMS.Application.Services.ActionEntity;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.ActionEntity
{
    // Title (required, max length) and Code (max length) rules come from the base class.
    // Add rules for parents etc. in the constructor, e.g. RuleFor(x => x.CategoryId).GreaterThan(0)...
    public sealed class CreateActionEntityDtoValidator : CatalogCreateValidator<ActionEntityCreateDto>
    {
        public CreateActionEntityDtoValidator() : base(ActionEntityService.EntityLabel, x => x.Code)
        {
            RuleFor(x => x.Description).MaximumLength(500).WithMessage("توضیحات اقدام نمی‌تواند بیشتر از 500 کاراکتر باشد.");
        }
    }
}
