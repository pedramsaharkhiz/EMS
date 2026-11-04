using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.ReportType
{
    public class ReportTypeCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
