using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class TechnicianAssignmentRepository : Repository<TechnicianAssignment>, ITechnicianAssignmentRepository
    {
        public TechnicianAssignmentRepository(EMSDbContext context) : base(context) { }

        public async Task<TechnicianAssignment?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default) =>
            await _dbSet.IncludingInactive().FirstOrDefaultAsync(x => x.TechnicianAssignmentId == id, cancellationToken);

        public IQueryable<TechnicianAssignmentRow> QueryRows() => ToRows(_dbSet.AsNoTracking().IncludingInactive());

        public async Task<TechnicianAssignmentRow?> GetRowAsync(long id, CancellationToken cancellationToken = default) =>
            await ToRows(_dbSet.AsNoTracking().IncludingInactive().Where(x => x.TechnicianAssignmentId == id)).FirstOrDefaultAsync(cancellationToken);

        public async Task<List<TechnicianAssignmentActionRow>> GetActionRowsAsync(long assignmentId, CancellationToken cancellationToken = default) =>
            await _context.TechnicianAssignmentActions.AsNoTracking().IncludingInactive()
                .Where(x => x.TechnicianAssignmentId == assignmentId)
                .OrderBy(x => x.ActionDateTime).ThenBy(x => x.TechnicianAssignmentActionId)
                .Select(x => new TechnicianAssignmentActionRow
                {
                    Id = x.TechnicianAssignmentActionId,
                    TechnicianAssignmentId = x.TechnicianAssignmentId,
                    ActionId = x.ActionId,
                    ActionTitle = x.Action.Title,
                    ActionCode = x.Action.Code,
                    EquipmentLocationHistoryId = x.EquipmentLocationHistoryId,
                    ActionDateTime = x.ActionDateTime,
                    Description = x.Description,
                    CreatedByUserId = x.CreatedByUserId,
                })
                .ToListAsync(cancellationToken);

        public async Task<bool> HasOpenForReportAsync(long failureReportId, CancellationToken cancellationToken = default) =>
            await _dbSet.IncludingInactive().AnyAsync(x => x.FailureReportId == failureReportId
                && TechnicianAssignmentCatalog.OpenIds.Contains(x.TechnicianAssignmentStatusId), cancellationToken);

        private static IQueryable<TechnicianAssignmentRow> ToRows(IQueryable<TechnicianAssignment> source) =>
            source.Select(a => new TechnicianAssignmentRow
            {
                Id = a.TechnicianAssignmentId,
                FailureReportId = a.FailureReportId,
                FailureReportReferenceNumber = a.FailureReport.ReferenceNumber,
                EquipmentId = a.FailureReport.EquipmentId,
                EquipmentYaasCode = a.FailureReport.Equipment.YaasCode,
                StatusId = a.TechnicianAssignmentStatusId,
                StatusCode = a.TechnicianAssignmentStatus.Code,
                StatusTitle = a.TechnicianAssignmentStatus.Title,
                TechnicianUserId = a.TechnicianUserId,
                AssignedByUserId = a.AssignedByUserId,
                AssignedDateTime = a.AssignedDateTime,
                DeadlineDateTime = a.DeadlineDateTime,
                CompletedDateTime = a.CompletedDateTime,
                Description = a.Description,
                ResultNote = a.ResultNote,
                ActionCount = a.TechnicianAssignmentActions.Count,
                CreatedDateTime = a.CreatedDateTime,
                ModifiedDateTime = a.ModifiedDateTime,
            });
    }
}
