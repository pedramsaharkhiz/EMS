using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class FailureTypeRepository : CatalogRepository<FailureType>, IFailureTypeRepository
    {
        public FailureTypeRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.FailureReports.AnyAsync(x => x.FailureTypeId == id && x.IsActive, cancellationToken);
        }

        public async Task<List<long>> GetEquipmentTypeIdsAsync(long failureTypeId, CancellationToken cancellationToken = default) =>
            await _context.FailureTypeEquipmentTypes.AsNoTracking().Where(l => l.FailureTypeId == failureTypeId)
                .Select(l => l.EquipmentTypeId).OrderBy(id => id).ToListAsync(cancellationToken);

        public async Task<Dictionary<long, List<(long Id, string Title)>>> GetEquipmentTypesAsync(IReadOnlyCollection<long> failureTypeIds, CancellationToken cancellationToken = default)
        {
            if (failureTypeIds.Count == 0)
                return new Dictionary<long, List<(long Id, string Title)>>();

            // the soft-delete filter of the equipment type applies: a deactivated equipment type is not offered
            var rows = await _context.FailureTypeEquipmentTypes.AsNoTracking()
                .Where(l => failureTypeIds.Contains(l.FailureTypeId))
                .Select(l => new { l.FailureTypeId, Id = l.EquipmentTypeId, l.EquipmentType.Title })
                .ToListAsync(cancellationToken);
            return rows.GroupBy(r => r.FailureTypeId)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Title, StringComparer.Ordinal).ThenBy(r => r.Id).Select(r => (r.Id, r.Title)).ToList());
        }

        public async Task ReplaceEquipmentTypesAsync(long failureTypeId, IReadOnlyCollection<long> equipmentTypeIds, CancellationToken cancellationToken = default)
        {
            var existing = await _context.FailureTypeEquipmentTypes.Where(l => l.FailureTypeId == failureTypeId).ToListAsync(cancellationToken);
            _context.FailureTypeEquipmentTypes.RemoveRange(existing.Where(l => !equipmentTypeIds.Contains(l.EquipmentTypeId)));
            _context.FailureTypeEquipmentTypes.AddRange(equipmentTypeIds
                .Where(id => existing.All(l => l.EquipmentTypeId != id))
                .Select(id => new FailureTypeEquipmentType { FailureTypeId = failureTypeId, EquipmentTypeId = id }));
        }
    }
}
