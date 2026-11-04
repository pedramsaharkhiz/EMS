using EMS.Application.Dtos.ReportType;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.ReportType
{
    public sealed class ReportTypeListRequestDtoValidator : CatalogListRequestValidator<ReportTypeListRequestDto>
    {
        // Paging and sort-direction rules come from the base class; add rules for your own filters here.
    }
}
