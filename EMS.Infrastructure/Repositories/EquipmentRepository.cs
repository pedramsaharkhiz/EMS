using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
    {
        public EquipmentRepository(EMSDbContext context) : base(context)
        {
        }

        public async Task<Equipment?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Include(x => x.EquipmentModel)
                .ThenInclude(x => x!.Brand)
                .Include(x => x.EquipmentModel)
                .ThenInclude(x => x!.EquipmentType)
                .ThenInclude(x => x!.EquipmentCategory)
                .Include(x => x.EquipmentModel)
                .Include(x => x.EquipmentStatus)
                .Include(x => x.CurrentEquipmentLocationType)
                .FirstOrDefaultAsync(x => x.EquipmentId == id, cancellationToken);
        }

        public async Task<List<Equipment>> GetByModelIdAsync(long equipmentModelId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Where(x => x.EquipmentModelId == equipmentModelId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Equipment>> GetByStatusIdAsync(long equipmentStatusId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Where(x => x.EquipmentStatusId == equipmentStatusId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Equipment>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<Equipment?> GetWithLatestLocationAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Include(x => x.EquipmentLocationHistories.OrderByDescending(h => h.CreatedDateTime))
                .FirstOrDefaultAsync(x => x.EquipmentId == id, cancellationToken);
        }
    }
}
