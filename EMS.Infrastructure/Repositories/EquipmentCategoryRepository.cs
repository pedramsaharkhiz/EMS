using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentCategoryRepository : Repository<EquipmentCategory>, IEquipmentCategoryRepository 
    { 
        public EquipmentCategoryRepository(EMSDbContext context) : base(context) { } 
        public async Task<EquipmentCategory?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default) 
        { 
            IQueryable<EquipmentCategory> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet; 
            return await query.FirstOrDefaultAsync(x => x.EquipmentCategoryId == id, cancellationToken);
        } 
        public async Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default)
        { 
            var normalizedTitle = title.Trim(); var query = _dbSet.AsQueryable();
            query = query.Where(x => x.Title.ToLower() == normalizedTitle.ToLower());
            if (exceptId.HasValue) 
            { 
                query = query.Where(x => x.EquipmentCategoryId != exceptId.Value);
            } 
            return await query.AnyAsync(cancellationToken);
        }
    }
}
