using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class FailureCategoryRepository : Repository<FailureCategory>, IFailureCategoryRepository
    {
        public FailureCategoryRepository(EMSDbContext context) : base(context) { }

        public async Task<FailureCategory?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            IQueryable<FailureCategory> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            return await query.FirstOrDefaultAsync(x => x.FailureCategoryId == id, cancellationToken);
        }
        public async Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var normalizedTitle = title.Trim(); var query = _dbSet.AsQueryable();
            query = query.Where(x => x.Title.ToLower() == normalizedTitle.ToLower());
            if (exceptId.HasValue)
            {
                query = query.Where(x => x.FailureCategoryId != exceptId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
    }
}
