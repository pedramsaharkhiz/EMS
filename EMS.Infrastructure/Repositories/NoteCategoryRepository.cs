using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class NoteCategoryRepository : CatalogRepository<NoteCategory>, INoteCategoryRepository
    {
        public NoteCategoryRepository(EMSDbContext context) : base(context) { }

        public override Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return _context.Set<EMS.Domain.Entities.AssignmentNote>().AnyAsync(x => x.NoteCategoryId == id && x.IsActive, cancellationToken);
        }
    }
}
