using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class ProvinceRepository : CatalogRepository<Province>, IProvinceRepository
    {
        public ProvinceRepository(EMSDbContext context) : base(context) { }

        // A province cannot be deactivated while an active city still belongs to it.
        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default) =>
            await _context.Cities.AnyAsync(x => x.ProvinceId == id && x.IsActive, cancellationToken);
    }
}
