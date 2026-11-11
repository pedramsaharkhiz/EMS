using EMS.Domain.Entities;
using EMS.Domain.ReadModels;

namespace EMS.Domain.Interfaces
{
    public interface ITechnicianAssignmentRepository : IRepository<TechnicianAssignment>
    {
        /// <summary>Loads an assignment for modification (tracked).</summary>
        Task<TechnicianAssignment?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>Read-only flattened assignments (all of them; assignments are never deactivated).</summary>
        IQueryable<TechnicianAssignmentRow> QueryRows();

        Task<TechnicianAssignmentRow?> GetRowAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>The actions recorded on an assignment, oldest first.</summary>
        Task<List<TechnicianAssignmentActionRow>> GetActionRowsAsync(long assignmentId, CancellationToken cancellationToken = default);

        /// <summary>The technicians who have an assignment on the report that is still open (assigned, accepted or in progress).</summary>
        Task<HashSet<long>> GetOpenTechnicianIdsOfReportAsync(long failureReportId, CancellationToken cancellationToken = default);
    }
}
