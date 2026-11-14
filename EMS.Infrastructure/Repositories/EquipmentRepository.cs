using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
    {
        private readonly long _currentUserId;

        /// <param name="currentUser">Rows are per caller (see <c>HasAssignmentOfCurrentUser</c>); null (hand-built in a test) means nobody.</param>
        public EquipmentRepository(EMSDbContext context, EMS.Application.Common.Interfaces.ICurrentUserService? currentUser = null) : base(context)
        {
            _currentUserId = currentUser?.UserId ?? 0;
        }

        public async Task<Equipment?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
        {
            // The catalog rows an equipment refers to (status, model, brand, ...) may have been deactivated since:
            // lift the soft-delete filter so the equipment is not dropped from the result by the join.
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .IncludingInactive()
                .Include(x => x.EquipmentModel)
                .ThenInclude(x => x!.Brand)
                .Include(x => x.EquipmentModel)
                .ThenInclude(x => x!.EquipmentType)
                .ThenInclude(x => x!.EquipmentCategory)
                .Include(x => x.EquipmentModel)
                .Include(x => x.EquipmentStatus)
                .FirstOrDefaultAsync(x => x.EquipmentId == id, cancellationToken);
        }

        public async Task<List<Equipment>> GetByModelIdAsync(long equipmentModelId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Where(x => x.EquipmentModelId == equipmentModelId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Equipment>> GetByStatusIdAsync(long equipmentStatusId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Where(x => x.EquipmentStatusId == equipmentStatusId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Equipment>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<Equipment?> GetWithLatestLocationAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Equipment>()
                .AsNoTracking()
                .Include(x => x.EquipmentLocationHistories.OrderByDescending(h => h.CreatedDateTime))
                .FirstOrDefaultAsync(x => x.EquipmentId == id, cancellationToken);
        }

        public async Task<Equipment?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default) =>
            await _dbSet.IncludingInactive().FirstOrDefaultAsync(x => x.EquipmentId == id, cancellationToken);

        public async Task<List<Equipment>> GetManyForUpdateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        {
            var wanted = ids.ToList();
            return await _dbSet.IncludingInactive().Where(x => wanted.Contains(x.EquipmentId)).ToListAsync(cancellationToken);
        }

        public IQueryable<EquipmentRow> QueryRows(bool active)
        {
            // The soft-delete filter is lifted for the whole query (so an equipment is not dropped by a join to a catalog row, a post or
            // a department that was deactivated afterwards) and the equipment's own state is asked for explicitly.
            var all = _dbSet.AsNoTracking().IncludingInactive();
            var source = active ? all.Where(x => x.IsActive) : all.Where(x => !x.IsActive);
            return ToRows(source);
        }

        public IQueryable<EquipmentRow> QueryRowsIncludingInactive() => ToRows(_dbSet.AsNoTracking().IncludingInactive());

        public async Task<EquipmentRow?> GetRowAsync(long id, CancellationToken cancellationToken = default) =>
            await ToRows(_dbSet.AsNoTracking().IncludingInactive().Where(x => x.EquipmentId == id)).FirstOrDefaultAsync(cancellationToken);

        private IQueryable<EquipmentRow> ToRows(IQueryable<Equipment> source) =>
            source.Select(e => new EquipmentRow
            {
                Id = e.EquipmentId,
                HasAssignmentOfCurrentUser = e.FailureReports.Any(f => f.TechnicianAssignments.Any(a => a.TechnicianUserId == _currentUserId)),
                Owner = e.Owner,
                OwnerTitle = e.Owner == EquipmentOwner.Company ? EquipmentOwnerTitles.Company : e.Owner == EquipmentOwner.Bank ? EquipmentOwnerTitles.Bank : EquipmentOwnerTitles.Unknown,
                YaasCode = e.YaasCode,
                SerialNumber = e.SerialNumber,
                AssetNumber = e.AssetNumber,

                EquipmentTypeId = e.EquipmentTypeId,
                EquipmentModelId = e.EquipmentModelId,
                EquipmentModelTitle = e.EquipmentModel!.Title,
                BrandId = (long?)e.EquipmentModel!.BrandId,
                BrandTitle = e.EquipmentModel!.Brand.Title,
                // An equipment has either a type or a model (check constraint): the effective type is its own or its model's.
                EffectiveTypeId = e.EquipmentTypeId ?? (long?)e.EquipmentModel!.EquipmentTypeId,
                EffectiveTypeTitle = e.EquipmentType != null ? e.EquipmentType.Title : e.EquipmentModel!.EquipmentType.Title,
                CategoryId = e.EquipmentType != null
                    ? (long?)e.EquipmentType.EquipmentCategoryId
                    : (long?)e.EquipmentModel!.EquipmentType.EquipmentCategoryId,
                CategoryTitle = e.EquipmentType != null
                    ? e.EquipmentType.EquipmentCategory.Title
                    : e.EquipmentModel!.EquipmentType.EquipmentCategory.Title,

                StatusId = e.EquipmentStatusId,
                StatusTitle = e.EquipmentStatus.Title,
                StatusCode = e.EquipmentStatus.Code,

                LocationKind = e.CurrentOrganizationId != null ? EquipmentLocationKind.Organization : EquipmentLocationKind.SupportingDepartment,
                LocationKindTitle = e.CurrentOrganizationId != null ? EquipmentLocationKindTitles.Organization : EquipmentLocationKindTitles.SupportingDepartment,
                OrganizationId = e.CurrentOrganizationId,
                OrganizationTitle = e.CurrentOrganization!.Title,
                FieldId = e.CurrentOrganization!.FieldId,
                FieldTitle = e.CurrentOrganization!.Field!.Title,
                OrganizationManagementId = e.CurrentOrganization!.OrganizationManagementId ?? (long?)e.CurrentOrganization!.Field!.OrganizationManagementId,
                OrganizationManagementTitle = e.CurrentOrganization!.OrganizationManagement != null
                    ? e.CurrentOrganization!.OrganizationManagement!.Title
                    : e.CurrentOrganization!.Field!.OrganizationManagement.Title,
                ZoneId = e.CurrentOrganization!.OrganizationManagement != null
                    ? (long?)e.CurrentOrganization!.OrganizationManagement!.ZoneId
                    : (long?)e.CurrentOrganization!.Field!.OrganizationManagement.ZoneId,
                ZoneTitle = e.CurrentOrganization!.OrganizationManagement != null
                    ? e.CurrentOrganization!.OrganizationManagement!.Zone.Title
                    : e.CurrentOrganization!.Field!.OrganizationManagement.Zone.Title,
                OrganizationSupportingDepartmentId = e.CurrentOrganization!.SupportingDepartmentId,
                OrganizationSupportingDepartmentTitle = e.CurrentOrganization!.SupportingDepartment!.Title,
                SupportingDepartmentId = e.CurrentSupportingDepartmentId,
                SupportingDepartmentTitle = e.CurrentSupportingDepartment!.Title,
                CityId = (long?)e.CurrentSupportingDepartment!.CityId,
                CityTitle = e.CurrentSupportingDepartment!.City.Title,
                ProvinceId = (long?)e.CurrentSupportingDepartment!.City.ProvinceId,
                ProvinceTitle = e.CurrentSupportingDepartment!.City.Province.Title,
                TechnicianUserId = e.TechnicianUserId,
                TechnicianName = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                TechnicianPersonnelCode = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.PersonnelCode).FirstOrDefault(),
                TechnicianIsActive = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => (bool?)u.IsActive).FirstOrDefault(),
                TechnicianPostId = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.PostId).FirstOrDefault(),
                TechnicianPostTitle = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.Post!.Title).FirstOrDefault(),
                TechnicianSupportingDepartmentId = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.SupportingDepartmentId).FirstOrDefault(),
                TechnicianSupportingDepartmentTitle = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.SupportingDepartment!.Title).FirstOrDefault(),
                TechnicianCityId = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => (long?)u.SupportingDepartment!.CityId).FirstOrDefault(),
                TechnicianCityTitle = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.SupportingDepartment!.City.Title).FirstOrDefault(),
                TechnicianProvinceId = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => (long?)u.SupportingDepartment!.City.ProvinceId).FirstOrDefault(),
                TechnicianProvinceTitle = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.SupportingDepartment!.City.Province.Title).FirstOrDefault(),

                Description = e.Description,
                IsActive = e.IsActive,
                CreatedDateTime = e.CreatedDateTime,
                ModifiedDateTime = e.ModifiedDateTime,
            });

        // Unique among ALL equipment: the soft-delete filter is lifted, a deactivated equipment keeps its numbers.
        //
        // On SQL Server the column's collation is case-insensitive (see docs/EQUIPMENT.md), so a plain "=" already ignores case AND can use the unique
        // index (a seek). Wrapping the column in LOWER() would make it read the whole table on every create and update. The in-memory provider that the
        // tests use compares case-sensitively, so there (and only there) the column is lowered to keep the same behavior.
        private bool CaseInsensitiveByCollation => _context.Database.IsSqlServer();

        public async Task<bool> SerialNumberExistsAsync(string serialNumber, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var value = serialNumber.Trim();
            var query = CaseInsensitiveByCollation
                ? _dbSet.IncludingInactive().Where(x => x.SerialNumber == value)
                : _dbSet.IncludingInactive().Where(x => x.SerialNumber != null && x.SerialNumber.ToLower() == value.ToLower());
            if (exceptId.HasValue)
            {
                var except = exceptId.Value;
                query = query.Where(x => x.EquipmentId != except);
            }
            return await query.AnyAsync(cancellationToken);
        }

        public async Task<bool> AssetNumberExistsAsync(string assetNumber, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var value = assetNumber.Trim();
            var query = CaseInsensitiveByCollation
                ? _dbSet.IncludingInactive().Where(x => x.AssetNumber == value)
                : _dbSet.IncludingInactive().Where(x => x.AssetNumber != null && x.AssetNumber.ToLower() == value.ToLower());
            if (exceptId.HasValue)
            {
                var except = exceptId.Value;
                query = query.Where(x => x.EquipmentId != except);
            }
            return await query.AnyAsync(cancellationToken);
        }

        public IQueryable<EquipmentLocationHistoryRow> QueryLocationHistory(long equipmentId) =>
            _context.EquipmentLocationHistories.AsNoTracking().IncludingInactive()
                .Where(h => h.EquipmentId == equipmentId)
                .OrderByDescending(h => h.AssignedDateTime).ThenByDescending(h => h.EquipmentLocationHistoryId)
                .Select(h => new EquipmentLocationHistoryRow
                {
                    Id = h.EquipmentLocationHistoryId,
                    EquipmentId = h.EquipmentId,
                    LocationKind = h.OrganizationId != null ? EquipmentLocationKind.Organization : EquipmentLocationKind.SupportingDepartment,
                    LocationKindTitle = h.OrganizationId != null ? EquipmentLocationKindTitles.Organization : EquipmentLocationKindTitles.SupportingDepartment,
                    OrganizationId = h.OrganizationId,
                    OrganizationTitle = h.Organization!.Title,
                    SupportingDepartmentId = h.SupportingDepartmentId,
                    SupportingDepartmentTitle = h.SupportingDepartment!.Title,
                    CityId = (long?)h.SupportingDepartment!.CityId,
                    CityTitle = h.SupportingDepartment!.City.Title,
                    ProvinceId = (long?)h.SupportingDepartment!.City.ProvinceId,
                    ProvinceTitle = h.SupportingDepartment!.City.Province.Title,
                    TechnicianUserId = h.TechnicianUserId,
                    AssignedDateTime = h.AssignedDateTime,
                    ChangedByUserId = h.ChangedByUserId,
                    Description = h.Description,
                });

        public void AddLocationHistory(EquipmentLocationHistory entry) => _context.EquipmentLocationHistories.Add(entry);

        public IQueryable<TechnicianInfo> QueryTechnicians() =>
            _context.Users.AsNoTracking().IncludingInactive()   // a department that was deactivated still names where the technician works
                .Where(u => u.IsActive)   // every active user: the post does not decide who can be a technician
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
                .Select(u => new TechnicianInfo
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    PersonnelCode = u.PersonnelCode,
                    SupportingDepartmentId = u.SupportingDepartmentId,
                    SupportingDepartmentTitle = u.SupportingDepartment!.Title,
                    CityId = (long?)u.SupportingDepartment!.CityId,
                    CityTitle = u.SupportingDepartment!.City.Title,
                    ProvinceId = (long?)u.SupportingDepartment!.City.ProvinceId,
                    ProvinceTitle = u.SupportingDepartment!.City.Province.Title,
                });

        public async Task<HashSet<long>> FindExistingUserIdsAsync(IReadOnlyCollection<long> userIds, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Distinct().ToList();
            var found = await _context.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => u.Id).ToListAsync(cancellationToken);
            return found.ToHashSet();
        }

        public async Task<EquipmentTechnicianRow?> GetTechnicianOfEquipmentAsync(long equipmentId, CancellationToken cancellationToken = default)
        {
            var technicianId = await _dbSet.IncludingInactive().AsNoTracking()
                .Where(e => e.EquipmentId == equipmentId).Select(e => e.TechnicianUserId).FirstOrDefaultAsync(cancellationToken);
            if (technicianId is null)
                return null;

            // The post and the department may have been deactivated since: lift the soft-delete filter so they still show.
            return await _context.Users.IncludingInactive().AsNoTracking()
                .Where(u => u.Id == technicianId)
                .Select(u => new EquipmentTechnicianRow
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    PersonnelCode = u.PersonnelCode,
                    IsActive = u.IsActive,
                    PostId = u.PostId,
                    PostTitle = u.Post!.Title,
                    SupportingDepartmentId = u.SupportingDepartmentId,
                    SupportingDepartmentTitle = u.SupportingDepartment!.Title,
                    CityId = (long?)u.SupportingDepartment!.CityId,
                    CityTitle = u.SupportingDepartment!.City.Title,
                    ProvinceId = (long?)u.SupportingDepartment!.City.ProvinceId,
                    ProvinceTitle = u.SupportingDepartment!.City.Province.Title,
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        // The values that already exist, lowered (the caller compares lowered values). See CaseInsensitiveByCollation for why SQL Server compares the raw column.
        public async Task<HashSet<string>> FindExistingSerialNumbersAsync(IReadOnlyCollection<string> serialNumbers, CancellationToken cancellationToken = default)
        {
            var values = serialNumbers.Select(c => c.Trim()).Distinct().ToList();
            List<string> found;
            if (CaseInsensitiveByCollation)
            {
                found = await _dbSet.IncludingInactive().Where(x => x.SerialNumber != null && values.Contains(x.SerialNumber))
                    .Select(x => x.SerialNumber!).ToListAsync(cancellationToken);
            }
            else
            {
                var lowered = values.Select(c => c.ToLower()).ToList();
                found = await _dbSet.IncludingInactive().Where(x => x.SerialNumber != null && lowered.Contains(x.SerialNumber.ToLower()))
                    .Select(x => x.SerialNumber!).ToListAsync(cancellationToken);
            }
            return found.Select(c => c.ToLower()).ToHashSet();
        }

        public async Task<HashSet<string>> FindExistingAssetNumbersAsync(IReadOnlyCollection<string> assetNumbers, CancellationToken cancellationToken = default)
        {
            var values = assetNumbers.Select(c => c.Trim()).Distinct().ToList();
            List<string> found;
            if (CaseInsensitiveByCollation)
            {
                found = await _dbSet.IncludingInactive().Where(x => x.AssetNumber != null && values.Contains(x.AssetNumber))
                    .Select(x => x.AssetNumber!).ToListAsync(cancellationToken);
            }
            else
            {
                var lowered = values.Select(c => c.ToLower()).ToList();
                found = await _dbSet.IncludingInactive().Where(x => x.AssetNumber != null && lowered.Contains(x.AssetNumber.ToLower()))
                    .Select(x => x.AssetNumber!).ToListAsync(cancellationToken);
            }
            return found.Select(c => c.ToLower()).ToHashSet();
        }

        public async Task<List<EquipmentRow>> GetRowsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        {
            var list = ids.ToList();
            return await QueryRows(true).Where(r => list.Contains(r.Id)).ToListAsync(cancellationToken);
        }
    }
}
