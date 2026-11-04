using EMS.Application.Dtos.FailureReportStatus;

namespace EMS.Application.Interfaces.FailureReportStatus
{
    public interface IFailureReportStatusService
        : ICatalogService<FailureReportStatusCreateDto, FailureReportStatusUpdateDto, FailureReportStatusDetailDto, FailureReportStatusListItemDto, FailureReportStatusListRequestDto>
    {
    }
}
