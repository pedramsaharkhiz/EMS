using EMS.Application.Dtos.ReportType;

namespace EMS.Application.Interfaces.ReportType
{
    public interface IReportTypeService
        : ICatalogService<ReportTypeCreateDto, ReportTypeUpdateDto, ReportTypeDetailDto, ReportTypeListItemDto, ReportTypeListRequestDto>
    {
    }
}
