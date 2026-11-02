using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentTypeRepository : Repository<EquipmentType>, IEquipmentTypeRepository
    {
        public EquipmentTypeRepository(EMSDbContext context) : base(context) { }
        public async Task<EquipmentType?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            IQueryable<EquipmentType> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            return await query.FirstOrDefaultAsync(x => x.EquipmentTypeId == id, cancellationToken);
        }
        public async Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var normalizedTitle = title.Trim();
            var query = _dbSet.AsQueryable();
            query = query.Where(x => x.Title.ToLower() == normalizedTitle.ToLower());
            if (exceptId.HasValue)
            {
                query = query.Where(x => x.EquipmentTypeId != exceptId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
    }
}
