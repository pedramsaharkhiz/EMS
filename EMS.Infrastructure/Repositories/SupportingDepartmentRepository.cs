using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class SupportingDepartmentRepository : Repository<SupportingDepartment>, ISupportingDepartmentRepository
    {
        public SupportingDepartmentRepository(EMSDbContext context) : base(context) { }

        public async Task<SupportingDepartment?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            IQueryable<SupportingDepartment> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            return await query.FirstOrDefaultAsync(x => x.SupportingDepartmentId == id, cancellationToken);
        }
        public async Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var normalizedTitle = title.Trim();
            var query = _dbSet.AsQueryable();
            query = query.Where(x => x.Title.ToLower() == normalizedTitle.ToLower());
            if (exceptId.HasValue)
            {
                query = query.Where(x => x.SupportingDepartmentId != exceptId.Value);
            }
            return await query.AnyAsync(cancellationToken);
        }
    }
}
