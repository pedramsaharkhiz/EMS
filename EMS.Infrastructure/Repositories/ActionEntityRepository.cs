using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class ActionEntityRepository : CatalogRepository<ActionEntity>, IActionEntityRepository
    {
        public ActionEntityRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.FailureReportActions.AnyAsync(x => x.ActionId == id && x.IsActive, cancellationToken)
                || await _context.TechnicianAssignmentActions.AnyAsync(x => x.ActionId == id && x.IsActive, cancellationToken);
        }
    }
}
