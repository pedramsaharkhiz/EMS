using EMS.Application.Dtos.ActionEntity;
using EMS.Application.Services.ActionEntity;
using EMS.Application.Validators.Catalog;
using FluentValidation;

namespace EMS.Application.Validators.ActionEntity
{
    // Title (max length) and Code (max length) rules come from the base class (the id comes from the route).
    public sealed class UpdateActionEntityDtoValidator : CatalogUpdateValidator<ActionEntityUpdateDto>
    {
        public UpdateActionEntityDtoValidator() : base(ActionEntityService.EntityLabel, x => x.Code)
        {
            RuleFor(x => x.Description).MaximumLength(500).WithMessage("توضیحات اقدام نمی‌تواند بیشتر از 500 کاراکتر باشد.");
        }
    }
}
