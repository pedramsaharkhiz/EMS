using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentLocationHistoryRepository : Repository<EquipmentLocationHistory>, IEquipmentLocationHistoryRepository
    {
        public EquipmentLocationHistoryRepository(EMSDbContext context) : base(context)
        {
        }

        public async Task<List<EquipmentLocationHistory>> GetHistoryByEquipmentIdAsync(long equipmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<EquipmentLocationHistory>()
                .AsNoTracking()
                .Where(x => x.EquipmentId == equipmentId)
                .OrderByDescending(x => x.CreatedDateTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<EquipmentLocationHistory?> GetLatestByEquipmentIdAsync(long equipmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<EquipmentLocationHistory>()
                .AsNoTracking()
                .Where(x => x.EquipmentId == equipmentId)
                .OrderByDescending(x => x.CreatedDateTime)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
