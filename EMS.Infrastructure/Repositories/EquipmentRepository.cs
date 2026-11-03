using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
    {
        public EquipmentRepository(EMSDbContext context) : base(context)
        {
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
                .Include(x => x.CurrentEquipmentLocationType)
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

        public IQueryable<EquipmentRow> QueryRows(bool active)
        {
            // Active: the soft-delete filter keeps only active equipment. Inactive: lift it (for the whole query, so a
            // deactivated equipment is not dropped by a join to a catalog row that was deactivated afterwards).
            var source = active
                ? _dbSet.AsNoTracking()
                : _dbSet.AsNoTracking().IncludingInactive().Where(x => !x.IsActive);
            return ToRows(source);
        }

        public async Task<EquipmentRow?> GetRowAsync(long id, CancellationToken cancellationToken = default) =>
            await ToRows(_dbSet.AsNoTracking().IncludingInactive().Where(x => x.EquipmentId == id)).FirstOrDefaultAsync(cancellationToken);

        private IQueryable<EquipmentRow> ToRows(IQueryable<Equipment> source) =>
            source.Select(e => new EquipmentRow
            {
                Id = e.EquipmentId,
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

                LocationTypeId = e.CurrentEquipmentLocationTypeId,
                LocationTypeTitle = e.CurrentEquipmentLocationType.Title,
                OrganizationId = e.CurrentOrganizationId,
                OrganizationTitle = e.CurrentOrganization!.Title,
                FieldId = e.CurrentOrganization!.FieldId,
                OrganizationManagementId = e.CurrentOrganization!.OrganizationManagementId ?? (long?)e.CurrentOrganization!.Field!.OrganizationManagementId,
                ZoneId = e.CurrentOrganization!.OrganizationManagement != null
                    ? (long?)e.CurrentOrganization!.OrganizationManagement!.ZoneId
                    : (long?)e.CurrentOrganization!.Field!.OrganizationManagement.ZoneId,
                SupportingDepartmentId = e.CurrentSupportingDepartmentId,
                SupportingDepartmentTitle = e.CurrentSupportingDepartment!.Title,
                CityId = (long?)e.CurrentSupportingDepartment!.CityId,
                CityTitle = e.CurrentSupportingDepartment!.City.Title,
                ProvinceId = (long?)e.CurrentSupportingDepartment!.City.ProvinceId,
                ProvinceTitle = e.CurrentSupportingDepartment!.City.Province.Title,
                TechnicianUserId = e.TechnicianUserId,
                TechnicianName = _context.Users.Where(u => u.Id == e.TechnicianUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),

                Description = e.Description,
                IsActive = e.IsActive,
                CreatedDateTime = e.CreatedDateTime,
                ModifiedDateTime = e.ModifiedDateTime,
            });

        public async Task<bool> YaasCodeExistsAsync(string yaasCode, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var normalized = yaasCode.Trim().ToLower();

            // The soft-delete filter keeps deactivated equipment out of this query: only active ones reserve a code
            // (consistent with the filtered unique index UX_Equipments_YaasCode).
            var query = _dbSet.Where(x => x.YaasCode.ToLower() == normalized);
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
                    LocationTypeId = h.EquipmentLocationTypeId,
                    LocationTypeTitle = h.EquipmentLocationType.Title,
                    OrganizationId = h.OrganizationId,
                    OrganizationTitle = h.Organization!.Title,
                    SupportingDepartmentId = h.SupportingDepartmentId,
                    SupportingDepartmentTitle = h.SupportingDepartment!.Title,
                    TechnicianUserId = h.TechnicianUserId,
                    TechnicianName = _context.Users.Where(u => u.Id == h.TechnicianUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    AssignedDateTime = h.AssignedDateTime,
                    ChangedByUserId = h.ChangedByUserId,
                    ChangedByName = _context.Users.Where(u => u.Id == h.ChangedByUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    Description = h.Description,
                });

        public void AddLocationHistory(EquipmentLocationHistory entry) => _context.EquipmentLocationHistories.Add(entry);

        public IQueryable<TechnicianInfo> QueryTechnicians() =>
            _context.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
                .Select(u => new TechnicianInfo { Id = u.Id, FullName = u.FirstName + " " + u.LastName, PersonnelCode = u.PersonnelCode });

        // Today every active user may be a technician. Change this (and QueryTechnicians) to restrict, e.g. by role or skill.
        public async Task<bool> IsEligibleTechnicianAsync(long userId, CancellationToken cancellationToken = default) =>
            await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        public async Task<bool> TechnicianExistsAsync(long userId, CancellationToken cancellationToken = default) =>
            await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
    }
}
