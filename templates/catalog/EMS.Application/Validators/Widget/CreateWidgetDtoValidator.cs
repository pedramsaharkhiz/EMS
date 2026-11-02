using EMS.Application.Dtos.Widget;
using EMS.Application.Services.Widget;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Widget
{
    // Title (required, max length) and Code (max length) rules come from the base class.
    // Add rules for parents etc. in the constructor, e.g. RuleFor(x => x.CategoryId).GreaterThan(0)...
    public sealed class CreateWidgetDtoValidator : CatalogCreateValidator<WidgetCreateDto>
    {
//#if (HasCode)
        public CreateWidgetDtoValidator() : base(WidgetService.EntityLabel, x => x.Code) { }
//#else
        public CreateWidgetDtoValidator() : base(WidgetService.EntityLabel) { }
//#endif
    }
}
