using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class SupportingDepartmentRepository : CatalogRepository<SupportingDepartment>, ISupportingDepartmentRepository
    {
        public SupportingDepartmentRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(x => x.SupportingDepartmentId == id && x.IsActive, cancellationToken)
                || await _context.Equipments.AnyAsync(x => x.CurrentSupportingDepartmentId == id && x.IsActive, cancellationToken)
                || await _context.Organizations.AnyAsync(x => x.SupportingDepartmentId == id && x.IsActive, cancellationToken);
        }

        public async Task<HashSet<long>> GetExistingIdsAsync(string level, IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        {
            var wanted = ids.Distinct().ToList();
            if (wanted.Count == 0)
                return new HashSet<long>();

            var found = level switch
            {
                "organization" => await _context.Organizations.Where(x => wanted.Contains(x.OrganizationId)).Select(x => x.OrganizationId).ToListAsync(cancellationToken),
                "field" => await _context.Fields.Where(x => wanted.Contains(x.FieldId)).Select(x => x.FieldId).ToListAsync(cancellationToken),
                "organizationmanagement" => await _context.OrganizationManagements.Where(x => wanted.Contains(x.OrganizationManagementId)).Select(x => x.OrganizationManagementId).ToListAsync(cancellationToken),
                "zone" => await _context.Zones.Where(x => wanted.Contains(x.ZoneId)).Select(x => x.ZoneId).ToListAsync(cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(level)),
            };
            return found.ToHashSet();
        }

        public async Task<List<long>> ResolveOrganizationIdsAsync(
            IReadOnlyCollection<long> fieldIds, IReadOnlyCollection<long> organizationManagementIds, IReadOnlyCollection<long> zoneIds,
            CancellationToken cancellationToken = default)
        {
            var fields = fieldIds.Distinct().ToList();
            var managements = organizationManagementIds.Distinct().ToList();
            var zones = zoneIds.Distinct().ToList();
            if (fields.Count == 0 && managements.Count == 0 && zones.Count == 0)
                return new List<long>();

            // A branch hangs under a field, or directly under a management (then it has no field).
            return await _context.Organizations
                .Where(o => o.IsActive && (
                    (o.FieldId != null && (fields.Contains(o.FieldId.Value)
                        || managements.Contains(o.Field!.OrganizationManagementId)
                        || zones.Contains(o.Field!.OrganizationManagement.ZoneId)))
                    || (o.FieldId == null && o.OrganizationManagementId != null && (managements.Contains(o.OrganizationManagementId.Value)
                        || zones.Contains(o.OrganizationManagement!.ZoneId)))))
                .Select(o => o.OrganizationId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Organization>> GetOrganizationsForUpdateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        {
            var wanted = ids.Distinct().ToList();
            return await _context.Organizations.Where(o => wanted.Contains(o.OrganizationId)).ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<long, string>> GetTitlesAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        {
            var wanted = ids.Distinct().ToList();
            if (wanted.Count == 0)
                return new Dictionary<long, string>();
            return await _dbSet.IncludingInactive().Where(d => wanted.Contains(d.SupportingDepartmentId))
                .Select(d => new { d.SupportingDepartmentId, d.Title })
                .ToDictionaryAsync(d => d.SupportingDepartmentId, d => d.Title, cancellationToken);
        }

        public async Task<int> CountOrganizationsAsync(long departmentId, CancellationToken cancellationToken = default) =>
            await _context.Organizations.CountAsync(o => o.SupportingDepartmentId == departmentId && o.IsActive, cancellationToken);
    }
}
