using EMS.Application.Dtos.Widget;
using EMS.Application.Services.Widget;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Widget
{
    // A positive id, Title (max length) and Code (max length) rules come from the base class.
    public sealed class UpdateWidgetDtoValidator : CatalogUpdateValidator<WidgetUpdateDto>
    {
//#if (HasCode)
        public UpdateWidgetDtoValidator() : base(WidgetService.EntityLabel, x => x.WidgetId, x => x.Code) { }
//#else
        public UpdateWidgetDtoValidator() : base(WidgetService.EntityLabel, x => x.WidgetId) { }
//#endif
    }
}
