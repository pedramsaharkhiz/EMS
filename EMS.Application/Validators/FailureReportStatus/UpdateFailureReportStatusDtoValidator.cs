using EMS.Application.Dtos.FailureReportStatus;
using EMS.Application.Services.FailureReportStatus;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.FailureReportStatus
{
    // Title (max length) and Code (max length) rules come from the base class (the id comes from the route).
    public sealed class UpdateFailureReportStatusDtoValidator : CatalogUpdateValidator<FailureReportStatusUpdateDto>
    {
        public UpdateFailureReportStatusDtoValidator() : base(FailureReportStatusService.EntityLabel, x => x.Code) { }
    }
}
