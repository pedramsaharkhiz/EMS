using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class FailureReportStatusRepository : CatalogRepository<FailureReportStatus>, IFailureReportStatusRepository
    {
        public FailureReportStatusRepository(EMSDbContext context) : base(context) { }

        public override Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            // TODO: return true while ACTIVE records still reference this one, e.g.
            //   return await _context.SomethingElse.AnyAsync(x => x.FailureReportStatusId == id && x.IsActive, cancellationToken);
            return Task.FromResult(false);
        }
    }
}
