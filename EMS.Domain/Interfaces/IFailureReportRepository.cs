using EMS.Domain.Entities;
using EMS.Domain.ReadModels;

namespace EMS.Domain.Interfaces
{
    public interface IFailureReportRepository : IRepository<FailureReport>
    {
        /// <summary>Loads a report for modification, active or not (a deactivated one can still be edited and restored).</summary>
        Task<FailureReport?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>Read-only flattened reports: the active ones, or with false the deactivated ones.</summary>
        IQueryable<FailureReportRow> QueryRows(bool active);

        /// <summary>Every report, active or not (for a record reached by its id).</summary>
        IQueryable<FailureReportRow> QueryRowsIncludingInactive();

        Task<FailureReportRow?> GetRowAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>Is the reference number used by another ACTIVE report? (any casing; a deactivated one does not reserve it)</summary>
        Task<bool> ReferenceNumberExistsAsync(string referenceNumber, long? exceptId = null, CancellationToken cancellationToken = default);

        /// <summary>The registering user must exist (any user, active or not, is a valid history).</summary>
        Task<string?> GetUserNameAsync(long userId, CancellationToken cancellationToken = default);
    }
}
