using EMS.Application.Dtos.Widget;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.Widget
{
    public sealed class WidgetListRequestDtoValidator : CatalogListRequestValidator<WidgetListRequestDto>
    {
        // Paging and sort-direction rules come from the base class; add rules for your own filters here.
    }
}
