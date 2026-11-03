using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    public class FailureReportRepository : Repository<FailureReport>, IFailureReportRepository
    {
        public FailureReportRepository(EMSDbContext context) : base(context)
        {
        }

        public async Task<FailureReport?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
        {
            // The equipment of a report may have been deactivated since: lift the soft-delete filter, otherwise the join to
            // Equipment would drop the report itself.
            return await _context.Set<FailureReport>()
                .AsNoTracking()
                .IncludingInactive()
                .Include(x => x.Equipment)
                .Include(x => x.FailureType)
                .Include(x => x.ReportType)
                .Include(x => x.Organization)
                .Include(x => x.Employee)
                .Include(x => x.TechnicianAssignments)
                .Include(x => x.FailureReportActions).ThenInclude(x => x.Action)
                .FirstOrDefaultAsync(x => x.FailureReportId == id, cancellationToken);
        }

        public async Task<List<FailureReport>> GetByOrganizationIdAsync(long organizationId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FailureReport>()
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<FailureReport>> GetByReportTypeIdAsync(long reportTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FailureReport>()
                .AsNoTracking()
                .Where(x => x.ReportTypeId == reportTypeId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<FailureReport>> GetByFailureTypeIdAsync(long failureTypeId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FailureReport>()
                .AsNoTracking()
                .Where(x => x.FailureTypeId == failureTypeId)
                .ToListAsync(cancellationToken);
        }

        //public async Task<List<FailureReport>> GetByStatusIdAsync(long failureReportStatusId, CancellationToken cancellationToken = default)
        //{
        //    return await _context.Set<FailureReport>()
        //        .AsNoTracking()
        //        .Where(x => x.FailureReportStatusId == failureReportStatusId)
        //        .ToListAsync(cancellationToken);
        //}

        public async Task<List<FailureReport>> GetOpenReportsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<FailureReport>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);
        }
    }
}