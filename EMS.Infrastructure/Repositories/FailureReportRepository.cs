using EMS.Domain;
using EMS.Domain.Enums;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class FailureReportRepository : Repository<FailureReport>, IFailureReportRepository
    {
        public FailureReportRepository(EMSDbContext context) : base(context)
        {
        }

        public async Task<FailureReport?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default) =>
            await _dbSet.IncludingInactive().FirstOrDefaultAsync(x => x.FailureReportId == id, cancellationToken);

        public IQueryable<FailureReportRow> QueryRows(bool active)
        {
            // Active: the soft-delete filter keeps only active reports. Inactive: lift it for the whole query, so a deactivated
            // report is not dropped by a join to a catalog row or an equipment that was deactivated afterwards.
            var source = active
                ? _dbSet.AsNoTracking().IncludingInactive().Where(x => x.IsActive)
                : _dbSet.AsNoTracking().IncludingInactive().Where(x => !x.IsActive);
            return ToRows(source);
        }

        public async Task<FailureReportRow?> GetRowAsync(long id, CancellationToken cancellationToken = default) =>
            await ToRows(_dbSet.AsNoTracking().IncludingInactive().Where(x => x.FailureReportId == id)).FirstOrDefaultAsync(cancellationToken);

        private IQueryable<FailureReportRow> ToRows(IQueryable<FailureReport> source) =>
            source.Select(r => new FailureReportRow
            {
                Id = r.FailureReportId,
                ReferenceNumber = r.ReferenceNumber,
                EquipmentId = r.EquipmentId,
                EquipmentYaasCode = r.Equipment.YaasCode,
                EquipmentSerialNumber = r.Equipment.SerialNumber,
                EquipmentAssetNumber = r.Equipment.AssetNumber,
                EquipmentOwner = r.Equipment.Owner,
                EquipmentOwnerTitle = r.Equipment.Owner == EquipmentOwner.Company ? EquipmentOwnerTitles.Company : r.Equipment.Owner == EquipmentOwner.Bank ? EquipmentOwnerTitles.Bank : EquipmentOwnerTitles.Unknown,
                // an equipment has either a type or a model: the effective type is its own or its model's (as in the equipment list)
                EquipmentTypeId = r.Equipment.EquipmentTypeId ?? (long?)r.Equipment.EquipmentModel!.EquipmentTypeId,
                EquipmentTypeTitle = r.Equipment.EquipmentType != null ? r.Equipment.EquipmentType.Title : r.Equipment.EquipmentModel!.EquipmentType.Title,
                EquipmentModelId = r.Equipment.EquipmentModelId,
                EquipmentModelTitle = r.Equipment.EquipmentModel!.Title,
                EquipmentStatusId = r.Equipment.EquipmentStatusId,
                EquipmentStatusTitle = r.Equipment.EquipmentStatus.Title,
                EquipmentStatusCode = r.Equipment.EquipmentStatus.Code,
                FailureTypeId = r.FailureTypeId,
                FailureTypeTitle = r.FailureType.Title,
                FailureTypeCode = r.FailureType.Code,
                FailureCategoryId = r.FailureType.FailureCategoryId,
                FailureCategoryTitle = r.FailureType.FailureCategory.Title,
                ReportTypeId = r.ReportTypeId,
                ReportTypeTitle = r.ReportType.Title,
                ReportTypeCode = r.ReportType.Code,
                StatusId = r.FailureReportStatusId,
                StatusTitle = r.FailureReportStatus.Title,
                StatusCode = r.FailureReportStatus.Code,
                OrganizationId = r.OrganizationId,
                OrganizationTitle = r.Organization.Title,
                SupportingDepartmentId = r.Organization.SupportingDepartmentId,
                SupportingDepartmentTitle = r.Organization.SupportingDepartment!.Title,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee != null ? r.Employee.FirstName + " " + r.Employee.LastName : null,
                EmployeePersonnelCode = r.Employee != null ? r.Employee.PersonnelCode : null,
                ReportedByUserId = r.ReportedByUserId,
                PriorityId = r.PriorityId,
                ReportedDateTime = r.ReportedDateTime,
                Description = r.Description,
                IsActive = r.IsActive,
                CreatedDateTime = r.CreatedDateTime,
                ModifiedDateTime = r.ModifiedDateTime,
            });

        public async Task<bool> ReferenceNumberExistsAsync(string referenceNumber, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var value = referenceNumber.Trim();
            // the soft-delete filter keeps deactivated reports out: only active ones reserve a number (like the filtered unique index).
            // SQL Server: the column collation is case-insensitive, so "=" ignores case and can use the unique index (LOWER(column) could not);
            // the in-memory provider of the tests is case-sensitive, so there the column is lowered.
            var query = _context.Database.IsSqlServer()
                ? _dbSet.Where(x => x.ReferenceNumber == value)
                : _dbSet.Where(x => x.ReferenceNumber != null && x.ReferenceNumber.ToLower() == value.ToLower());
            if (exceptId.HasValue)
            {
                var except = exceptId.Value;
                query = query.Where(x => x.FailureReportId != except);
            }
            return await query.AnyAsync(cancellationToken);
        }

        public async Task<string?> GetUserNameAsync(long userId, CancellationToken cancellationToken = default) =>
            await _context.Users.Where(u => u.Id == userId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefaultAsync(cancellationToken);
    }
}
