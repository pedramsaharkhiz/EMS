using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class BrandRepository : Repository<Brand>, IBrandRepository
    {
        public BrandRepository(EMSDbContext context) : base(context) { }

        public async Task<Brand?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            IQueryable<Brand> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            return await query.FirstOrDefaultAsync(x => x.BrandId == id, cancellationToken);
        }
        public async Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var normalizedTitle = title.Trim();
            var query = _dbSet.AsQueryable();
            query = query.Where(x => x.Title.ToLower() == normalizedTitle.ToLower());
            if (exceptId.HasValue)
            {
                query = query.Where(x => x.BrandId != exceptId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
    }
}
