using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentLocationTypeRepository : CatalogRepository<EquipmentLocationType>, IEquipmentLocationTypeRepository
    {
        public EquipmentLocationTypeRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Equipments.AnyAsync(x => x.CurrentEquipmentLocationTypeId == id && x.IsActive, cancellationToken);
        }
    }
}
