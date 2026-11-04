using EMS.Application.Common.Models;
using EMS.Application.Dtos.FailureReport;

namespace EMS.Application.Interfaces.FailureReport
{
    public interface IFailureReportService
    {
        Task<PagedResult<FailureReportListItemDto>> GetPagedAsync(FailureReportListRequestDto request, CancellationToken cancellationToken = default);
        Task<FailureReportDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<FailureReportDetailDto> CreateAsync(FailureReportCreateDto request, CancellationToken cancellationToken = default);
        Task<FailureReportDetailDto> UpdateAsync(long id, FailureReportUpdateDto request, CancellationToken cancellationToken = default);

        /// <summary>Soft delete: deactivates the report (idempotent). Restore with an update with <c>isActive = true</c>.</summary>
        Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
