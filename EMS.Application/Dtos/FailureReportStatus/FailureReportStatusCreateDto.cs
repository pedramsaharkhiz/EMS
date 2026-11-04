using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.FailureReportStatus
{
    public class FailureReportStatusCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
