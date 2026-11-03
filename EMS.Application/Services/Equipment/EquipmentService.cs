using EMS.Application.Common.Catalog;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Export;
using EMS.Application.Common.Filtering;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Dtos.EquipmentLocationHistory;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.Equipment;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EquipmentEntity = EMS.Domain.Entities.Equipment;
using EquipmentLocationHistoryEntity = EMS.Domain.Entities.EquipmentLocationHistory;
using OrganizationEntity = EMS.Domain.Entities.Organization;

namespace EMS.Application.Services.Equipment
{
    /// <summary>
    /// Equipment: list (filters, sorting, paging), detail, create, update, soft delete / restore, move with location history,
    /// the technician lookup and the Excel export. Equipment is not a catalog (no unique title, real business fields), so it has its
    /// own service, but it follows the same conventions: <c>id</c> and <c>rowVersion</c>, Persian messages, stable error codes,
    /// soft delete through <c>IsActive</c>, audit entries.
    /// Open business rules live in one place each: <see cref="IEquipmentLocationRule"/> and <see cref="EquipmentStatusTransitions"/>.
    /// </summary>
    public sealed class EquipmentService : IEquipmentService
    {
        private const string FirstLocationNote = "ثبت اولیه تجهیز";

        // The filters of the list and of the export: one line each (add a filter = add a line and a property to the request).
        private static readonly FilterBuilder<EquipmentRow, EquipmentListRequestDto> Filters = new FilterBuilder<EquipmentRow, EquipmentListRequestDto>()
            .Search(r => r.Search, x => x.YaasCode, x => x.SerialNumber, x => x.AssetNumber)
            .Contains(r => r.YaasCode, x => x.YaasCode)
            .Contains(r => r.SerialNumber, x => x.SerialNumber)
            .Contains(r => r.AssetNumber, x => x.AssetNumber)
            .AnyOf(r => r.StatusIds, x => x.StatusId)
            .AnyOf(r => r.CategoryIds, x => x.CategoryId)
            .AnyOf(r => r.TypeIds, x => x.EffectiveTypeId)
            .AnyOf(r => r.ModelIds, x => x.EquipmentModelId)
            .AnyOf(r => r.BrandIds, x => x.BrandId)
            .AnyOf(r => r.LocationTypeIds, x => x.LocationTypeId)
            .AnyOf(r => r.OrganizationIds, x => x.OrganizationId)
            .AnyOf(r => r.FieldIds, x => x.FieldId)
            .AnyOf(r => r.OrganizationManagementIds, x => x.OrganizationManagementId)
            .AnyOf(r => r.ZoneIds, x => x.ZoneId)
            .AnyOf(r => r.SupportingDepartmentIds, x => x.SupportingDepartmentId)
            .AnyOf(r => r.CityIds, x => x.CityId)
            .AnyOf(r => r.ProvinceIds, x => x.ProvinceId)
            .AnyOf(r => r.TechnicianUserIds, x => x.TechnicianUserId)
            .AtLeast(r => r.CreatedFrom, x => x.CreatedDateTime)
            .AtMost(r => r.CreatedTo, x => x.CreatedDateTime);

        // The sortBy values the list accepts (the default order is by id).
        private static readonly SortBuilder<EquipmentRow> Sorting = new SortBuilder<EquipmentRow>()
            .By("yaascode", x => x.YaasCode)
            .By("serialnumber", x => x.SerialNumber)
            .By("assetnumber", x => x.AssetNumber)
            .By("category", x => x.CategoryTitle)
            .By("type", x => x.EffectiveTypeTitle)
            .By("model", x => x.EquipmentModelTitle)
            .By("brand", x => x.BrandTitle)
            .By("status", x => x.StatusTitle)
            .By("locationtype", x => x.LocationTypeTitle)
            .By("organization", x => x.OrganizationTitle)
            .By("department", x => x.SupportingDepartmentTitle)
            .By("city", x => x.CityTitle)
            .By("province", x => x.ProvinceTitle)
            .By("technician", x => x.TechnicianName)
            .By("createddatetime", x => x.CreatedDateTime);

        private readonly IEquipmentRepository _repository;
        private readonly IEquipmentStatusRepository _statuses;
        private readonly IEquipmentTypeRepository _types;
        private readonly IEquipmentModelRepository _models;
        private readonly IEquipmentLocationTypeRepository _locationTypes;
        private readonly ISupportingDepartmentRepository _departments;
        private readonly IRepository<OrganizationEntity> _organizations;
        private readonly IEquipmentLocationRule _locationRule;
        private readonly IExcelExporter _exporter;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;

        public EquipmentService(
            IEquipmentRepository repository,
            IEquipmentStatusRepository statuses,
            IEquipmentTypeRepository types,
            IEquipmentModelRepository models,
            IEquipmentLocationTypeRepository locationTypes,
            ISupportingDepartmentRepository departments,
            IRepository<OrganizationEntity> organizations,
            IEquipmentLocationRule locationRule,
            IExcelExporter exporter,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            IAuditEventLogger audit)
        {
            _repository = repository;
            _statuses = statuses;
            _types = types;
            _models = models;
            _locationTypes = locationTypes;
            _departments = departments;
            _organizations = organizations;
            _locationRule = locationRule;
            _exporter = exporter;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _audit = audit;
        }

        // ------------------------------------------------------------------------------------------------ read

        public async Task<PagedResult<EquipmentListItemDto>> GetPagedAsync(EquipmentListRequestDto request, CancellationToken cancellationToken = default)
        {
            var page = await Query(request).ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
            return new PagedResult<EquipmentListItemDto>
            {
                Items = page.Items.Select(row => Fill(new EquipmentListItemDto(), row)).ToList(),
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
            };
        }

        public Task<EquipmentDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            DetailAsync(id, cancellationToken);

        public async Task<ExcelFile> ExportAsync(EquipmentListRequestDto request, CancellationToken cancellationToken = default) =>
            await _exporter.ExportAsync(Query(request), EquipmentExport.Definition, cancellationToken);

        public async Task<PagedResult<EquipmentLocationHistoryItemDto>> GetLocationHistoryAsync(long id, EquipmentLocationHistoryRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsIncludingInactiveAsync(x => x.EquipmentId == id, cancellationToken))
                throw NotFound();

            var page = await _repository.QueryLocationHistory(id).ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
            return new PagedResult<EquipmentLocationHistoryItemDto>
            {
                Items = page.Items.Select(h => new EquipmentLocationHistoryItemDto
                {
                    Id = h.Id,
                    EquipmentLocationTypeId = h.LocationTypeId,
                    EquipmentLocationTypeTitle = h.LocationTypeTitle,
                    OrganizationId = h.OrganizationId,
                    OrganizationTitle = h.OrganizationTitle,
                    SupportingDepartmentId = h.SupportingDepartmentId,
                    SupportingDepartmentTitle = h.SupportingDepartmentTitle,
                    TechnicianUserId = h.TechnicianUserId,
                    TechnicianName = h.TechnicianName,
                    AssignedDateTime = h.AssignedDateTime,
                    ChangedByUserId = h.ChangedByUserId,
                    ChangedByName = h.ChangedByName,
                    Description = h.Description,
                }).ToList(),
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
            };
        }

        public async Task<PagedResult<TechnicianItemDto>> GetTechniciansAsync(TechnicianListRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _repository.QueryTechnicians();
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(t => t.FullName.Contains(term) || t.PersonnelCode.Contains(term));
            }

            var page = await query.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
            return new PagedResult<TechnicianItemDto>
            {
                Items = page.Items.Select(t => new TechnicianItemDto { Id = t.Id, FullName = t.FullName, PersonnelCode = t.PersonnelCode }).ToList(),
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
            };
        }

        // ------------------------------------------------------------------------------------------------ write

        public async Task<EquipmentDetailDto> CreateAsync(EquipmentCreateDto request, CancellationToken cancellationToken = default)
        {
            var yaasCode = request.YaasCode.Trim();
            var location = new EquipmentLocation(request.CurrentEquipmentLocationTypeId, request.CurrentOrganizationId, request.CurrentSupportingDepartmentId, request.TechnicianUserId);

            await RequireClassificationAsync(request.EquipmentStatusId, request.EquipmentTypeId, request.EquipmentModelId, mustBeActive: true, cancellationToken);
            await RequireLocationAsync(location, cancellationToken);
            await EnsureYaasCodeIsFreeAsync(yaasCode, exceptId: null, cancellationToken);

            var entity = new EquipmentEntity
            {
                YaasCode = yaasCode,
                SerialNumber = Normalize(request.SerialNumber),
                AssetNumber = Normalize(request.AssetNumber),
                EquipmentTypeId = request.EquipmentTypeId,
                EquipmentModelId = request.EquipmentModelId,
                EquipmentStatusId = request.EquipmentStatusId,
                CurrentEquipmentLocationTypeId = location.LocationTypeId,
                CurrentOrganizationId = location.OrganizationId,
                CurrentSupportingDepartmentId = location.SupportingDepartmentId,
                TechnicianUserId = location.TechnicianUserId,
                Description = Normalize(request.Description),
            };

            await _repository.AddAsync(entity, cancellationToken);
            _repository.AddLocationHistory(NewHistory(entity, location, FirstLocationNote));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Created("Equipment", entity.EquipmentId);
            return await DetailAsync(entity.EquipmentId, cancellationToken);
        }

        public async Task<EquipmentDetailDto> UpdateAsync(long id, EquipmentUpdateDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound();

            var yaasCode = request.YaasCode.Trim();
            var reactivating = !entity.IsActive && request.IsActive;
            var statusChanged = entity.EquipmentStatusId != request.EquipmentStatusId;

            // References must be active when the record is (and still will be) active and the reference is new or the record is
            // being restored; for a record that stays deactivated they only have to exist.
            var checkActive = request.IsActive && (reactivating || statusChanged
                || entity.EquipmentTypeId != request.EquipmentTypeId || entity.EquipmentModelId != request.EquipmentModelId);
            await RequireClassificationAsync(request.EquipmentStatusId, request.EquipmentTypeId, request.EquipmentModelId, checkActive, cancellationToken);

            if (reactivating)
                await RequireLocationAsync(new EquipmentLocation(entity.CurrentEquipmentLocationTypeId, entity.CurrentOrganizationId, entity.CurrentSupportingDepartmentId, entity.TechnicianUserId), cancellationToken, validateRule: false);

            if (statusChanged && !EquipmentStatusTransitions.IsAllowed(entity.EquipmentStatusId, request.EquipmentStatusId))
                throw new BadRequestException("تغییر وضعیت تجهیز به وضعیت انتخاب‌شده مجاز نیست.", ErrorCodes.StatusTransitionNotAllowed);

            // Only an active equipment reserves its Yaas code (consistent with the filtered unique index).
            if (request.IsActive && (reactivating || !string.Equals(entity.YaasCode, yaasCode, StringComparison.OrdinalIgnoreCase)))
                await EnsureYaasCodeIsFreeAsync(yaasCode, id, cancellationToken);

            entity.YaasCode = yaasCode;
            entity.SerialNumber = Normalize(request.SerialNumber);
            entity.AssetNumber = Normalize(request.AssetNumber);
            entity.EquipmentTypeId = request.EquipmentTypeId;
            entity.EquipmentModelId = request.EquipmentModelId;
            entity.EquipmentStatusId = request.EquipmentStatusId;
            entity.Description = Normalize(request.Description);

            ExpectVersion(entity, request.RowVersion);
            entity.IsActive = request.IsActive;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Updated("Equipment", id);
            return await DetailAsync(id, cancellationToken);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound();

            entity.IsActive = false;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Deleted("Equipment", id);
        }

        public async Task<EquipmentDetailDto> MoveAsync(long id, EquipmentMoveDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound();
            var location = new EquipmentLocation(request.CurrentEquipmentLocationTypeId, request.CurrentOrganizationId, request.CurrentSupportingDepartmentId, request.TechnicianUserId);

            // Moving to where it already is changes nothing and adds no history entry.
            if (location == new EquipmentLocation(entity.CurrentEquipmentLocationTypeId, entity.CurrentOrganizationId, entity.CurrentSupportingDepartmentId, entity.TechnicianUserId))
                return await DetailAsync(id, cancellationToken);

            await RequireLocationAsync(location, cancellationToken);

            entity.CurrentEquipmentLocationTypeId = location.LocationTypeId;
            entity.CurrentOrganizationId = location.OrganizationId;
            entity.CurrentSupportingDepartmentId = location.SupportingDepartmentId;
            entity.TechnicianUserId = location.TechnicianUserId;

            ExpectVersion(entity, request.RowVersion);
            _repository.Update(entity);
            _repository.AddLocationHistory(NewHistory(entity, location, Normalize(request.Description)));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Operation("LocationChanged", "Equipment", id, $"locationType={location.LocationTypeId}");
            return await DetailAsync(id, cancellationToken);
        }

        // ------------------------------------------------------------------------------------------------ helpers

        private IQueryable<EquipmentRow> Query(EquipmentListRequestDto request)
        {
            var rows = Filters.Apply(_repository.QueryRows(request.IsActive ?? true), request);

            var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            if (Sorting.TryApply(request.SortBy, descending, rows, out var sorted))
                return sorted;

            return descending ? rows.OrderByDescending(x => x.Id) : rows.OrderBy(x => x.Id);
        }

        private async Task<EquipmentDetailDto> DetailAsync(long id, CancellationToken cancellationToken)
        {
            var row = await _repository.GetRowAsync(id, cancellationToken) ?? throw NotFound();
            var detail = Fill(new EquipmentDetailDto(), row);
            detail.ModifiedDateTime = row.ModifiedDateTime;
            detail.RowVersion = RowVersionToken.From(row.ModifiedDateTime);
            return detail;
        }

        private static TDto Fill<TDto>(TDto dto, EquipmentRow row) where TDto : EquipmentListItemDto
        {
            dto.Id = row.Id;
            dto.YaasCode = row.YaasCode;
            dto.SerialNumber = row.SerialNumber;
            dto.AssetNumber = row.AssetNumber;
            dto.EquipmentTypeId = row.EquipmentTypeId;
            dto.EquipmentModelId = row.EquipmentModelId;
            dto.EquipmentModelTitle = row.EquipmentModelTitle;
            dto.BrandId = row.BrandId;
            dto.BrandTitle = row.BrandTitle;
            dto.EffectiveTypeId = row.EffectiveTypeId;
            dto.EffectiveTypeTitle = row.EffectiveTypeTitle;
            dto.CategoryId = row.CategoryId;
            dto.CategoryTitle = row.CategoryTitle;
            dto.EquipmentStatusId = row.StatusId;
            dto.EquipmentStatusTitle = row.StatusTitle;
            dto.CurrentEquipmentLocationTypeId = row.LocationTypeId;
            dto.CurrentEquipmentLocationTypeTitle = row.LocationTypeTitle;
            dto.CurrentOrganizationId = row.OrganizationId;
            dto.CurrentOrganizationTitle = row.OrganizationTitle;
            dto.CurrentSupportingDepartmentId = row.SupportingDepartmentId;
            dto.CurrentSupportingDepartmentTitle = row.SupportingDepartmentTitle;
            dto.CityId = row.CityId;
            dto.CityTitle = row.CityTitle;
            dto.ProvinceId = row.ProvinceId;
            dto.ProvinceTitle = row.ProvinceTitle;
            dto.TechnicianUserId = row.TechnicianUserId;
            dto.TechnicianName = row.TechnicianName;
            dto.Description = row.Description;
            dto.IsActive = row.IsActive;
            dto.CreatedDateTime = row.CreatedDateTime;
            return dto;
        }

        private async Task RequireClassificationAsync(long statusId, long? typeId, long? modelId, bool mustBeActive, CancellationToken cancellationToken)
        {
            await ParentGuard.RequireAsync(_statuses, statusId, x => x.EquipmentStatusId, "وضعیت تجهیز", mustBeActive, cancellationToken);
            if (typeId is { } type)
                await ParentGuard.RequireAsync(_types, type, x => x.EquipmentTypeId, "نوع تجهیز", mustBeActive, cancellationToken);
            if (modelId is { } model)
                await ParentGuard.RequireAsync(_models, model, x => x.EquipmentModelId, "مدل تجهیز", mustBeActive, cancellationToken);
        }

        private async Task RequireLocationAsync(EquipmentLocation location, CancellationToken cancellationToken, bool validateRule = true)
        {
            await ParentGuard.RequireAsync(_locationTypes, location.LocationTypeId, x => x.EquipmentLocationTypeId, "نوع مکان", mustBeActive: true, cancellationToken);
            if (location.OrganizationId is { } organization)
                await ParentGuard.RequireAsync(_organizations, organization, x => x.OrganizationId, "سازمان", mustBeActive: true, cancellationToken);
            if (location.SupportingDepartmentId is { } department)
                await ParentGuard.RequireAsync(_departments, department, x => x.SupportingDepartmentId, "دپارتمان پشتیبانی", mustBeActive: true, cancellationToken);

            if (location.TechnicianUserId is { } technician)
            {
                if (!await _repository.TechnicianExistsAsync(technician, cancellationToken))
                    throw new NotFoundException("تکنسین یافت نشد.", ErrorCodes.ParentNotFound);
                if (!await _repository.IsEligibleTechnicianAsync(technician, cancellationToken))
                    throw new BadRequestException("تکنسین انتخاب‌شده غیرفعال است.", ErrorCodes.ParentInactive);
            }

            if (validateRule)
                await _locationRule.ValidateAsync(location, cancellationToken);
        }

        private async Task EnsureYaasCodeIsFreeAsync(string yaasCode, long? exceptId, CancellationToken cancellationToken)
        {
            if (await _repository.YaasCodeExistsAsync(yaasCode, exceptId, cancellationToken))
                throw new ConflictException("تجهیز فعال دیگری با این یاس کد وجود دارد.", ErrorCodes.DuplicateYaasCode);
        }

        private EquipmentLocationHistoryEntity NewHistory(EquipmentEntity equipment, EquipmentLocation location, string? description) =>
            new()
            {
                Equipment = equipment,
                EquipmentLocationTypeId = location.LocationTypeId,
                OrganizationId = location.OrganizationId,
                SupportingDepartmentId = location.SupportingDepartmentId,
                TechnicianUserId = location.TechnicianUserId,
                AssignedDateTime = DateTime.UtcNow,
                ChangedByUserId = _currentUser.UserId,
                Description = description,
            };

        private void ExpectVersion(EquipmentEntity entity, string? rowVersion)
        {
            if (RowVersionToken.TryParse(rowVersion, out var expected))
                _repository.SetExpectedModifiedDateTime(entity, expected);
        }

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static NotFoundException NotFound() => new("تجهیز موردنظر پیدا نشد.", ErrorCodes.NotFound);
    }
}
