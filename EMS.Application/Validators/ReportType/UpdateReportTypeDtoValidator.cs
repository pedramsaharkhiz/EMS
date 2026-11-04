using EMS.Application.Dtos.ReportType;
using EMS.Application.Services.ReportType;
using EMS.Application.Validators.Catalog;

namespace EMS.Application.Validators.ReportType
{
    // Title (max length) and Code (max length) rules come from the base class (the id comes from the route).
    public sealed class UpdateReportTypeDtoValidator : CatalogUpdateValidator<ReportTypeUpdateDto>
    {
        public UpdateReportTypeDtoValidator() : base(ReportTypeService.EntityLabel, x => x.Code) { }
    }
}
