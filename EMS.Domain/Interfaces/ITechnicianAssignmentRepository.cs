using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface ITechnicianAssignmentRepository : IRepository<TechnicianAssignment>
    {
        Task<TechnicianAssignment?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);


        Task<List<TechnicianAssignment>> GetByTechnicianUserIdAsync(long technicianUserId, CancellationToken cancellationToken = default);

        Task<List<TechnicianAssignment>> GetActiveByTechnicianUserIdAsync(long technicianUserId, CancellationToken cancellationToken = default);

        Task<List<TechnicianAssignment>> GetByFailureReportIdAsync(long failureReportId, CancellationToken cancellationToken = default);

        Task<bool> ExistsActiveAsync(long technicianUserId, long failureReportId, CancellationToken cancellationToken = default);
    }
}
