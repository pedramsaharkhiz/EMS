using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class BrandRepository : CatalogRepository<Brand>, IBrandRepository
    {
        public BrandRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.EquipmentModels.AnyAsync(x => x.BrandId == id && x.IsActive, cancellationToken);
        }
    }
}
