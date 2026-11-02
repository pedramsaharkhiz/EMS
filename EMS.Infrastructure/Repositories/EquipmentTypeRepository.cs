using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentTypeRepository : CatalogRepository<EquipmentType>, IEquipmentTypeRepository
    {
        public EquipmentTypeRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.EquipmentModels.AnyAsync(x => x.EquipmentTypeId == id && x.IsActive, cancellationToken) || await _context.Equipments.AnyAsync(x => x.EquipmentTypeId == id && x.IsActive, cancellationToken);
        }
    }
}
