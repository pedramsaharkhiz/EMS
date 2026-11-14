using EMS.Application.Common.Catalog;
using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Export;
using EMS.Application.Common.Filtering;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Common.People;
using EMS.Application.Dtos.Dashboard;
using EMS.Application.Dtos.FailureReport;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.FailureReport;
using EMS.Domain;
using Microsoft.EntityFrameworkCore;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EmployeeEntity = EMS.Domain.Entities.Employee;
using FailureReportEntity = EMS.Domain.Entities.FailureReport;
using OrganizationEntity = EMS.Domain.Entities.Organization;

namespace EMS.Application.Services.FailureReport
{
    /// <summary>
    /// Failure reports (گزارش خرابی): list (filters, sorting, paging), detail, create, update, soft delete / restore. Like equipment it
    /// is not a catalog (no unique title), so it has its own service with the same conventions: <c>id</c> and <c>rowVersion</c>,
    /// Persian messages, stable error codes, soft delete through <c>IsActive</c>, audit entries.
    /// A report starts as Open and is attributed to the signed-in user. No rule limits status changes or ties a report to the
    /// equipment's status or location (the business has not asked for any).
    /// </summary>
    public sealed class FailureReportService : IFailureReportService
    {
        // The filters of the list: one line each (add a filter = add a line and a property to the request).
        private static readonly FilterBuilder<FailureReportRow, FailureReportListRequestDto> Filters = new FilterBuilder<FailureReportRow, FailureReportListRequestDto>()
            .Search(r => r.Search, x => x.ReferenceNumber, x => x.Description, x => x.EquipmentYaasCode)
            .Contains(r => r.ReferenceNumber, x => x.ReferenceNumber)
            .AnyOf(r => r.EquipmentIds, x => x.EquipmentId)
            .AnyOf(r => r.FailureTypeIds, x => x.FailureTypeId)
            .AnyOf(r => r.FailureCategoryIds, x => x.FailureCategoryId)
            .AnyOf(r => r.ReportTypeIds, x => x.ReportTypeId)
            .AnyOf(r => r.StatusIds, x => x.StatusId)
            .AnyOf(r => r.PriorityIds, x => x.PriorityId)
            .AnyOf(r => r.OrganizationIds, x => x.OrganizationId)
            .AnyOf(r => r.SupportingDepartmentIds, x => x.SupportingDepartmentId)
            .AnyOf(r => r.EmployeeIds, x => x.EmployeeId)
            .Eq(r => r.HasOpenAssignment, x => x.HasOpenAssignment)
            .AnyOf(r => r.ReportedByUserIds, x => x.ReportedByUserId)
            .AtLeast(r => r.ReportedFrom, x => x.ReportedDateTime)
            .AtMost(r => r.ReportedTo, x => x.ReportedDateTime);

        // The sortBy values the list accepts (the default order is by id).
        private static readonly SortBuilder<FailureReportRow> Sorting = new SortBuilder<FailureReportRow>()
            .By("referencenumber", x => x.ReferenceNumber)
            .By("equipment", x => x.EquipmentYaasCode)
            .By("failuretype", x => x.FailureTypeTitle)
            .By("failurecategory", x => x.FailureCategoryTitle)
            .By("reporttype", x => x.ReportTypeTitle)
            .By("status", x => x.StatusTitle)
            .By("organization", x => x.OrganizationTitle)
            .By("priority", x => x.PriorityId)
            .By("reporteddatetime", x => x.ReportedDateTime)
            .By("createddatetime", x => x.CreatedDateTime);

        private readonly IFailureReportRepository _repository;
        private readonly IEquipmentRepository _equipment;
        private readonly IFailureTypeRepository _failureTypes;
        private readonly IReportTypeRepository _reportTypes;
        private readonly IFailureReportStatusRepository _statuses;
        private readonly IRepository<OrganizationEntity> _organizations;
        private readonly IRepository<EmployeeEntity> _employees;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;
        private readonly IExcelExporter _exporter;
        private readonly IRowAccess _access;
        private readonly IUserReferenceRepository _people;

        public FailureReportService(
            IFailureReportRepository repository,
            IEquipmentRepository equipment,
            IFailureTypeRepository failureTypes,
            IReportTypeRepository reportTypes,
            IFailureReportStatusRepository statuses,
            IRepository<OrganizationEntity> organizations,
            IRepository<EmployeeEntity> employees,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            IAuditEventLogger audit,
            IExcelExporter exporter,
            IRowAccess access,
            IUserReferenceRepository people)
        {
            _repository = repository;
            _equipment = equipment;
            _failureTypes = failureTypes;
            _reportTypes = reportTypes;
            _statuses = statuses;
            _organizations = organizations;
            _employees = employees;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _audit = audit;
            _exporter = exporter;
            _access = access;
            _people = people;
        }

        // ------------------------------------------------------------------------------------------------ read

        public async Task<PagedResult<FailureReportListItemDto>> GetPagedAsync(FailureReportListRequestDto request, CancellationToken cancellationToken = default)
        {
            var page = await Query(request).ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

            var items = page.Items.Select(row => Fill(new FailureReportListItemDto(), row)).ToList();
            await FillReporterDepartmentsAsync(items, cancellationToken);

            return new PagedResult<FailureReportListItemDto>
            {
                Items = items,
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
            };
        }

        public async Task<FailureReportDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            var closedId = FailureReportStatusCatalog.IdOf(FailureReportStatusCode.Closed);
            var rows = VisibleRows(true);

            var byStatus = (await rows.GroupBy(r => r.StatusId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync(cancellationToken))
                .ToDictionary(x => x.Id, x => x.Count);
            var byPriority = (await rows.Where(r => r.StatusId != closedId).GroupBy(r => r.PriorityId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync(cancellationToken))
                .ToDictionary(x => x.Id, x => x.Count);
            var withoutAssignment = await rows.CountAsync(r => r.StatusId != closedId && !r.HasOpenAssignment, cancellationToken);

            var statuses = await _statuses.QueryByActive(true).OrderBy(x => x.FailureReportStatusId).ToListAsync(cancellationToken);
            var inactiveWithReports = byStatus.Keys.Except(statuses.Select(x => x.FailureReportStatusId)).ToList();
            if (inactiveWithReports.Count > 0)
                statuses.AddRange(await _statuses.QueryByActive(false).Where(x => inactiveWithReports.Contains(x.FailureReportStatusId)).ToListAsync(cancellationToken));

            return new FailureReportDashboardDto
            {
                Total = byStatus.Values.Sum(),
                UnresolvedCount = byStatus.Where(x => x.Key != closedId).Sum(x => x.Value),
                UnresolvedWithoutAssignmentCount = withoutAssignment,
                ByStatus = statuses.OrderBy(x => x.FailureReportStatusId).Select(x => new FailureReportStatusCountDto
                {
                    Id = x.FailureReportStatusId,
                    Code = x.Code,
                    Title = x.Title,
                    Count = byStatus.GetValueOrDefault(x.FailureReportStatusId),
                    IsUnresolved = x.FailureReportStatusId != closedId,
                }).ToList(),
                UnresolvedByPriority = Enumerable.Range(FailureReportPriorities.Min, FailureReportPriorities.Max - FailureReportPriorities.Min + 1)
                    .Select(id => new DashboardPriorityCountDto { Id = id, Title = FailureReportPriorities.TitleOf(id), Count = byPriority.GetValueOrDefault(id) }).ToList(),
            };
        }

        /// <summary>The Excel file of exactly the reports the list shows for the same filters and sorting (paging is ignored).</summary>
        public async Task<ExcelFile> ExportAsync(FailureReportListRequestDto request, CancellationToken cancellationToken = default) =>
            await _exporter.ExportAsync(Query(request), FailureReportExport.Definition, cancellationToken);

        /// <summary>
        /// THE way to reach the rows of the list and of the Excel export: the same filters, the same sorting, the same source.
        /// Anything that decides WHICH reports a caller may see (a future row-level security rule) belongs here, once, so that the grid
        /// and the file can never disagree. Do not query <see cref="IFailureReportRepository.QueryRows"/> from anywhere else in this service.
        /// </summary>
        /// <summary>
        /// THE source of failure report rows for the list, the Excel export AND the dashboard counts (a future row-level security rule goes here, once).
        /// </summary>
        private IQueryable<FailureReportRow> VisibleRows(bool active) => _access.Apply(_repository.QueryRows(active));

        private IQueryable<FailureReportRow> Query(FailureReportListRequestDto request)
        {
            var rows = Filters.Apply(VisibleRows(request.IsActive ?? true), request);

            var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            if (Sorting.TryApply(request.SortBy, descending, rows, out var sorted))
                return sorted;

            return descending ? rows.OrderByDescending(x => x.Id) : rows.OrderBy(x => x.Id);
        }

        public async Task<FailureReportDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            await EnsureVisibleAsync(id, cancellationToken);
            return await DetailAsync(id, cancellationToken);
        }

        // ------------------------------------------------------------------------------------------------ write

        public async Task<FailureReportDetailDto> CreateAsync(FailureReportCreateDto request, CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAppException("احراز هویت انجام نشده یا توکن نامعتبر است.");
            var statusId = (long)FailureReportStatusCode.Open;
            var referenceNumber = Normalize(request.ReferenceNumber);

            await RequireReferencesAsync(request.EquipmentId, request.FailureTypeId, request.ReportTypeId, statusId, request.OrganizationId, request.EmployeeId, mustBeActive: true, cancellationToken);
            if (referenceNumber is not null)
                await EnsureReferenceNumberIsFreeAsync(referenceNumber, exceptId: null, cancellationToken);

            var entity = new FailureReportEntity
            {
                EquipmentId = request.EquipmentId,
                FailureTypeId = request.FailureTypeId,
                ReportTypeId = request.ReportTypeId,
                FailureReportStatusId = statusId,
                OrganizationId = request.OrganizationId,
                EmployeeId = request.EmployeeId,
                ReportedByUserId = userId,
                ReferenceNumber = referenceNumber,
                PriorityId = request.PriorityId,
                ReportedDateTime = request.ReportedDateTime.HasValue ? ToUtc(request.ReportedDateTime.Value) : DateTime.UtcNow,
                Description = Normalize(request.Description),
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Created("FailureReport", entity.FailureReportId);
            return await DetailAsync(entity.FailureReportId, cancellationToken);
        }

        public async Task<FailureReportDetailDto> UpdateAsync(long id, FailureReportUpdateDto request, CancellationToken cancellationToken = default)
        {
            await EnsureVisibleAsync(id, cancellationToken);
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound();

            var reactivating = !entity.IsActive && request.IsActive;
            var referenceNumber = Normalize(request.ReferenceNumber);

            // A reference must be active when the record is (and still will be) active and the reference is new or the record is
            // being restored; for a record that stays deactivated it only has to exist.
            await RequireReferencesAsync(
                request.EquipmentId, request.FailureTypeId, request.ReportTypeId, request.FailureReportStatusId, request.OrganizationId, request.EmployeeId,
                mustBeActive: reactivating, cancellationToken,
                current: entity, checkChangedActive: request.IsActive);

            // Only an active report reserves its reference number (consistent with the filtered unique index).
            if (request.IsActive && referenceNumber is not null
                && (reactivating || !string.Equals(entity.ReferenceNumber, referenceNumber, StringComparison.OrdinalIgnoreCase)))
                await EnsureReferenceNumberIsFreeAsync(referenceNumber, id, cancellationToken);

            entity.EquipmentId = request.EquipmentId;
            entity.FailureTypeId = request.FailureTypeId;
            entity.ReportTypeId = request.ReportTypeId;
            entity.FailureReportStatusId = request.FailureReportStatusId;
            entity.OrganizationId = request.OrganizationId;
            entity.EmployeeId = request.EmployeeId;
            entity.ReferenceNumber = referenceNumber;
            entity.PriorityId = request.PriorityId;
            if (request.ReportedDateTime.HasValue)
                entity.ReportedDateTime = ToUtc(request.ReportedDateTime.Value);
            entity.Description = Normalize(request.Description);

            ExpectVersion(entity, request.RowVersion);
            entity.IsActive = request.IsActive;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Updated("FailureReport", id);
            return await DetailAsync(id, cancellationToken);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            await EnsureVisibleAsync(id, cancellationToken);
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound();

            entity.IsActive = false;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Deleted("FailureReport", id);
        }

        // ------------------------------------------------------------------------------------------------ helpers

        private async Task<FailureReportDetailDto> DetailAsync(long id, CancellationToken cancellationToken)
        {
            var row = await _repository.GetRowAsync(id, cancellationToken) ?? throw NotFound();      // row-access: every caller checked the record's visibility first
            var detail = Fill(new FailureReportDetailDto(), row);
            await FillReporterDepartmentsAsync(new[] { detail }, cancellationToken);
            detail.ModifiedDateTime = row.ModifiedDateTime;
            detail.RowVersion = RowVersionToken.From(row.ModifiedDateTime);
            return detail;
        }

        /// <summary>The reporter's departments (a list) come from the person lookup, one query for all the rows; the rows themselves only carry the texts of the Excel file.</summary>
        private async Task FillReporterDepartmentsAsync(IEnumerable<FailureReportListItemDto> items, CancellationToken cancellationToken)
        {
            var list = items.ToList();
            if (list.Count == 0)
                return;

            var people = await _people.GetPeopleAsync(list.Select(i => (long?)i.ReportedByUserId), cancellationToken);
            foreach (var item in list)
                PersonReferenceMapper.FillDepartments(item, "ReportedBy", people.GetValueOrDefault(item.ReportedByUserId));
        }

        private static TDto Fill<TDto>(TDto dto, FailureReportRow row) where TDto : FailureReportListItemDto
        {
            dto.Id = row.Id;
            dto.ReferenceNumber = row.ReferenceNumber;
            dto.EquipmentId = row.EquipmentId;
            dto.EquipmentYaasCode = row.EquipmentYaasCode;
            dto.EquipmentHasConflict = row.EquipmentHasConflict;
            dto.EquipmentSerialNumber = row.EquipmentSerialNumber;
            dto.EquipmentAssetNumber = row.EquipmentAssetNumber;
            dto.EquipmentOwner = row.EquipmentOwner;
            dto.EquipmentOwnerTitle = row.EquipmentOwnerTitle;
            dto.EquipmentTypeId = row.EquipmentTypeId;
            dto.EquipmentTypeTitle = row.EquipmentTypeTitle;
            dto.EquipmentModelId = row.EquipmentModelId;
            dto.EquipmentModelTitle = row.EquipmentModelTitle;
            dto.EquipmentStatusId = row.EquipmentStatusId;
            dto.EquipmentStatusTitle = row.EquipmentStatusTitle;
            dto.EquipmentStatusCode = row.EquipmentStatusCode;
            dto.FailureTypeId = row.FailureTypeId;
            dto.FailureTypeTitle = row.FailureTypeTitle;
            dto.FailureTypeCode = row.FailureTypeCode;
            dto.FailureCategoryId = row.FailureCategoryId;
            dto.FailureCategoryTitle = row.FailureCategoryTitle;
            dto.ReportTypeId = row.ReportTypeId;
            dto.ReportTypeTitle = row.ReportTypeTitle;
            dto.ReportTypeCode = row.ReportTypeCode;
            dto.FailureReportStatusId = row.StatusId;
            dto.FailureReportStatusTitle = row.StatusTitle;
            dto.FailureReportStatusCode = row.StatusCode;
            dto.OrganizationId = row.OrganizationId;
            dto.OrganizationTitle = row.OrganizationTitle;
            dto.SupportingDepartmentId = row.SupportingDepartmentId;
            dto.SupportingDepartmentTitle = row.SupportingDepartmentTitle;
            dto.EmployeeId = row.EmployeeId;
            dto.EmployeeName = row.EmployeeName;
            dto.EmployeePersonnelCode = row.EmployeePersonnelCode;
            dto.HasOpenAssignment = row.HasOpenAssignment;
            dto.ReportedByUserId = row.ReportedByUserId;
            dto.ReportedByName = row.ReportedByName;
            dto.ReportedByPersonnelCode = row.ReportedByPersonnelCode;
            dto.ReportedByIsActive = row.ReportedByIsActive;
            dto.ReportedByPostId = row.ReportedByPostId;
            dto.ReportedByPostTitle = row.ReportedByPostTitle;
            dto.PriorityId = row.PriorityId;
            dto.PriorityTitle = row.PriorityTitle;
            dto.ReportedDateTime = row.ReportedDateTime;
            dto.Description = row.Description;
            dto.IsActive = row.IsActive;
            dto.CreatedDateTime = row.CreatedDateTime;
            return dto;
        }

        /// <summary>
        /// Every referenced record must exist (404 <c>parent_not_found</c>) and, when asked, be active (400 <c>parent_inactive</c>).
        /// On update (<paramref name="current"/> given) a reference that did not change only has to exist, so a report keeps working
        /// after its equipment or catalog row was deactivated, unless it is being restored.
        /// </summary>
        private async Task RequireReferencesAsync(
            long equipmentId, long failureTypeId, long reportTypeId, long statusId, long organizationId, long? employeeId,
            bool mustBeActive, CancellationToken cancellationToken,
            FailureReportEntity? current = null, bool checkChangedActive = false)
        {
            bool Active(bool changed) => mustBeActive || (checkChangedActive && changed);

            await ParentGuard.RequireAsync(_equipment, equipmentId, x => x.EquipmentId, "تجهیز", Active(current is null || current.EquipmentId != equipmentId), cancellationToken);
            await ParentGuard.RequireAsync(_failureTypes, failureTypeId, x => x.FailureTypeId, "نوع خرابی", Active(current is null || current.FailureTypeId != failureTypeId), cancellationToken);
            await ParentGuard.RequireAsync(_reportTypes, reportTypeId, x => x.ReportTypeId, "نوع گزارش", Active(current is null || current.ReportTypeId != reportTypeId), cancellationToken);
            await ParentGuard.RequireAsync(_statuses, statusId, x => x.FailureReportStatusId, "وضعیت گزارش خرابی", Active(current is null || current.FailureReportStatusId != statusId), cancellationToken);
            await ParentGuard.RequireAsync(_organizations, organizationId, x => x.OrganizationId, "شعبه", Active(current is null || current.OrganizationId != organizationId), cancellationToken);
            if (employeeId is { } employee)
                await ParentGuard.RequireAsync(_employees, employee, x => x.EmployeeId, "کارمند", Active(current is null || current.EmployeeId != employeeId), cancellationToken);

            // The branch of a new or changed report, and its equipment, must be ones the caller may see (the answer for a hidden one is the same as for a missing one).
            if (current is null || current.OrganizationId != organizationId)
                await RequireBranchVisibleAsync(organizationId, cancellationToken);
            if (current is null || current.EquipmentId != equipmentId)
                await _access.RequireVisibleAsync(_equipment.QueryRowsIncludingInactive(), r => r.Id == equipmentId, "Equipment", equipmentId, "تجهیز یافت نشد.", cancellationToken, ErrorCodes.ParentNotFound);
        }

        /// <summary>A report is filed for a branch under the caller's department: the report policy applied to a row that does not exist yet (404 <c>parent_not_found</c> otherwise).</summary>
        private async Task RequireBranchVisibleAsync(long organizationId, CancellationToken cancellationToken)
        {
            if (_access.Scope.IsUnrestricted)
                return;

            var department = (await _organizations.FindAsync(o => o.OrganizationId == organizationId, asNoTracking: true, cancellationToken: cancellationToken)).FirstOrDefault()?.SupportingDepartmentId;
            if (!_access.IsVisible(new FailureReportRow { SupportingDepartmentId = department }))
                throw new NotFoundException("شعبه یافت نشد.", ErrorCodes.ParentNotFound);
        }

        /// <summary>A record reached by its id must be one the caller may see (404 otherwise, as for a missing one). Call it BEFORE loading the record.</summary>
        private Task EnsureVisibleAsync(long id, CancellationToken cancellationToken) =>
            _access.RequireVisibleAsync(_repository.QueryRowsIncludingInactive(), r => r.Id == id, "FailureReport", id, NotFound().Message, cancellationToken);

        private async Task EnsureReferenceNumberIsFreeAsync(string referenceNumber, long? exceptId, CancellationToken cancellationToken)
        {
            if (await _repository.ReferenceNumberExistsAsync(referenceNumber, exceptId, cancellationToken))
                throw new ConflictException("گزارش فعال دیگری با این شماره ارجاع وجود دارد.", ErrorCodes.DuplicateReferenceNumber);
        }

        private void ExpectVersion(FailureReportEntity entity, string? rowVersion)
        {
            if (RowVersionToken.TryParse(rowVersion, out var expected))
                _repository.SetExpectedModifiedDateTime(entity, expected);
        }

        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static NotFoundException NotFound() => new("گزارش خرابی موردنظر پیدا نشد.", ErrorCodes.NotFound);
    }
}
