using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class TechnicianAssignmentRepository : Repository<TechnicianAssignment>, ITechnicianAssignmentRepository
    {
        public TechnicianAssignmentRepository(EMSDbContext context) : base(context)
        {
        }

        public async Task<TechnicianAssignment?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<TechnicianAssignment>()
                .AsNoTracking()
                .Include(x => x.FailureReport)
                .Include(x => x.TechnicianAssignmentStatus)
                .Include(x => x.TechnicianAssignmentActions)
                .FirstOrDefaultAsync(x => x.TechnicianAssignmentId == id, cancellationToken);
        }

        public async Task<List<TechnicianAssignment>> GetByTechnicianUserIdAsync(long technicianUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<TechnicianAssignment>()
                .AsNoTracking()
                .Where(x => x.TechnicianUserId == technicianUserId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TechnicianAssignment>> GetActiveByTechnicianUserIdAsync(long technicianUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<TechnicianAssignment>()
                .AsNoTracking()
                .Where(x => x.TechnicianUserId == technicianUserId && x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TechnicianAssignment>> GetByFailureReportIdAsync(long failureReportId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<TechnicianAssignment>()
                .AsNoTracking()
                .Where(x => x.FailureReportId == failureReportId)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsActiveAsync(long technicianUserId, long failureReportId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<TechnicianAssignment>()
                .AnyAsync(x =>
                    x.TechnicianUserId == technicianUserId &&
                    x.FailureReportId == failureReportId &&
                    x.IsActive,
                    cancellationToken);
        }
    }
}
