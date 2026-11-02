using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class FailureCategoryRepository : CatalogRepository<FailureCategory>, IFailureCategoryRepository
    {
        public FailureCategoryRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.FailureTypes.AnyAsync(x => x.FailureCategoryId == id && x.IsActive, cancellationToken);
        }
    }
}
