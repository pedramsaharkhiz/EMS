using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentTypeRepository : CatalogRepository<EquipmentType>, IEquipmentTypeRepository
    {
        public EquipmentTypeRepository(EMSDbContext context) : base(context) { }

        // The soft-delete filter is lifted for the whole query so a deactivated category does not hide the type; the type's own state is explicit.
        public IQueryable<EquipmentTypeWithCategoryRow> QueryWithoutModels() =>
            _context.EquipmentTypes.AsNoTracking().IncludingInactive()
                .Where(t => t.IsActive && !_context.EquipmentModels.Any(m => m.EquipmentTypeId == t.EquipmentTypeId && m.IsActive))
                .Select(t => new EquipmentTypeWithCategoryRow
                {
                    Id = t.EquipmentTypeId,
                    Title = t.Title,
                    Code = t.Code,
                    IsActive = t.IsActive,
                    EquipmentCategoryId = t.EquipmentCategoryId,
                    EquipmentCategoryTitle = t.EquipmentCategory.Title,
                });

        public IQueryable<EquipmentTypeWithCategoryRow> QueryTypeOfModel(long modelId) =>
            _context.EquipmentModels.AsNoTracking().IncludingInactive()
                .Where(m => m.EquipmentModelId == modelId)
                .Select(m => new EquipmentTypeWithCategoryRow
                {
                    Id = m.EquipmentTypeId,
                    Title = m.EquipmentType.Title,
                    Code = m.EquipmentType.Code,
                    IsActive = m.EquipmentType.IsActive,
                    EquipmentCategoryId = m.EquipmentType.EquipmentCategoryId,
                    EquipmentCategoryTitle = m.EquipmentType.EquipmentCategory.Title,
                });

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.EquipmentModels.AnyAsync(x => x.EquipmentTypeId == id && x.IsActive, cancellationToken) || await _context.Equipments.AnyAsync(x => x.EquipmentTypeId == id && x.IsActive, cancellationToken);
        }
    }
}
