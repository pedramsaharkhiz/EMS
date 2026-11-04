using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class ReportTypeRepository : CatalogRepository<ReportType>, IReportTypeRepository
    {
        public ReportTypeRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.FailureReports.AnyAsync(x => x.ReportTypeId == id && x.IsActive, cancellationToken);
        }
    }
}
