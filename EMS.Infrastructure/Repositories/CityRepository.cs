using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class CityRepository : CatalogRepository<City>, ICityRepository
    {
        public CityRepository(EMSDbContext context) : base(context) { }

        public async Task<CityLocation?> GetLocationAsync(long cityId, CancellationToken cancellationToken = default) =>
            await _dbSet.AsNoTracking().IncludingInactive()
                .Where(x => x.CityId == cityId)
                .Select(x => new CityLocation(x.CityId, x.Title, x.ProvinceId, x.Province.Title))
                .FirstOrDefaultAsync(cancellationToken);

        // A city cannot be deactivated while an active support department still belongs to it.
        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default) =>
            await _context.SupportingDepartments.AnyAsync(x => x.CityId == id && x.IsActive, cancellationToken);
    }
}
