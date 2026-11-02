using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IFailureReportRepository : IRepository<FailureReport>
    {
        Task<FailureReport?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);

        Task<List<FailureReport>> GetByOrganizationIdAsync(long organizationId, CancellationToken cancellationToken = default);

        Task<List<FailureReport>> GetByReportTypeIdAsync(long reportTypeId, CancellationToken cancellationToken = default);

        Task<List<FailureReport>> GetByFailureTypeIdAsync(long failureTypeId, CancellationToken cancellationToken = default);

        //Task<List<FailureReport>> GetByStatusIdAsync(long failureReportStatusId, CancellationToken cancellationToken = default);

        Task<List<FailureReport>> GetOpenReportsAsync(CancellationToken cancellationToken = default);
    }
}
