using EMS.Application.Dtos.FailureReportStatus;
using EMS.Application.Services.FailureReportStatus;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.FailureReportStatus
{
    // Title (required, max length) and Code (max length) rules come from the base class.
    // Add rules for parents etc. in the constructor, e.g. RuleFor(x => x.CategoryId).GreaterThan(0)...
    public sealed class CreateFailureReportStatusDtoValidator : CatalogCreateValidator<FailureReportStatusCreateDto>
    {
        public CreateFailureReportStatusDtoValidator() : base(FailureReportStatusService.EntityLabel, x => x.Code) { }
    }
}
