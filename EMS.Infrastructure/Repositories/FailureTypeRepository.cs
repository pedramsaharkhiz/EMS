using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class FailureTypeRepository : CatalogRepository<FailureType>, IFailureTypeRepository
    {
        public FailureTypeRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.FailureReports.AnyAsync(x => x.FailureTypeId == id && x.IsActive, cancellationToken);
        }
    }
}
