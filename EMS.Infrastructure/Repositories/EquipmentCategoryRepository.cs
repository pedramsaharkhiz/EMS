using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentCategoryRepository : CatalogRepository<EquipmentCategory>, IEquipmentCategoryRepository
    {
        public EquipmentCategoryRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.EquipmentTypes.AnyAsync(x => x.EquipmentCategoryId == id && x.IsActive, cancellationToken);
        }
    }
}
