using EMS.Application.Dtos.FailureReportStatus;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.FailureReportStatus
{
    public sealed class FailureReportStatusListRequestDtoValidator : CatalogListRequestValidator<FailureReportStatusListRequestDto>
    {
        // Paging and sort-direction rules come from the base class; add rules for your own filters here.
    }
}
