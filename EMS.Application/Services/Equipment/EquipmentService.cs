using EMS.Application.Common.Catalog;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Export;
using EMS.Application.Common.Filtering;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Attachment;
using EMS.Application.Interfaces.Attachment;
using EMS.Application.Services.Attachment;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Dtos.EquipmentLocationHistory;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.Equipment;
using EMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        private const string NotTechnicianMessage = "کاربر انتخاب‌شده سمت «تکنسین» ندارد و نمی‌تواند تکنسین تجهیز باشد.";
        private const string AttachmentsTable = "Equipments";
        private const string FirstLocationNote = "ثبت اولیه تجهیز";

        // The filters of the list and of the export: one line each (add a filter = add a line and a property to the request).
        private static readonly FilterBuilder<EquipmentRow, EquipmentListRequestDto> Filters = new FilterBuilder<EquipmentRow, EquipmentListRequestDto>()
            .Search(r => r.Search, x => x.YaasCode, x => x.SerialNumber, x => x.AssetNumber)
            .Contains(r => r.YaasCode, x => x.YaasCode)
            .Contains(r => r.SerialNumber, x => x.SerialNumber)
            .Contains(r => r.AssetNumber, x => x.AssetNumber)
            .AnyOf(r => r.Owners, x => x.Owner)
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
            .AnyOf(r => r.OrganizationSupportingDepartmentIds, x => x.OrganizationSupportingDepartmentId)
            .AnyOf(r => r.CityIds, x => x.CityId)
            .AnyOf(r => r.ProvinceIds, x => x.ProvinceId)
            .AnyOf(r => r.TechnicianUserIds, x => x.TechnicianUserId)
            .AtLeast(r => r.CreatedFrom, x => x.CreatedDateTime)
            .AtMost(r => r.CreatedTo, x => x.CreatedDateTime);

        // The sortBy values the list accepts (the default order is by id).
        private static readonly SortBuilder<EquipmentRow> Sorting = new SortBuilder<EquipmentRow>()
            .By("owner", x => x.Owner)
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
        private readonly IAttachmentService _attachments;

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
            IAuditEventLogger audit,
            IAttachmentService attachments)
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
            _attachments = attachments;
        }

        // ------------------------------------------------------------------------------------------------ read

        public async Task<PagedResult<EquipmentListItemDto>> GetPagedAsync(EquipmentListRequestDto request, CancellationToken cancellationToken = default)
        {
            var page = await Query(request).ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

            // one grouped query for the whole page, not one per row
            var counts = await _attachments.CountActiveAsync(AttachmentsTable, page.Items.Select(r => r.Id).ToList(), cancellationToken);
            var items = page.Items.Select(row => Fill(new EquipmentListItemDto(), row)).ToList();
            foreach (var item in items)
                item.AttachmentCount = counts.GetValueOrDefault(item.Id);

            return new PagedResult<EquipmentListItemDto>
            {
                Items = items,
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
            };
        }

        public async Task<EquipmentDetailWithAttachmentsDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var detail = await DetailAsync(id, cancellationToken);
            var result = new EquipmentDetailWithAttachmentsDto();
            CopyDetail(detail, result);

            // an equipment that was deactivated still lists its files (the permission is the same Equipments.View)
            result.Attachments = await _attachments.ListActiveAsync(AttachmentsTable, id, cancellationToken);
            result.AttachmentCount = result.Attachments.Count;
            return result;
        }

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
                    CityId = h.CityId,
                    CityTitle = h.CityTitle,
                    ProvinceId = h.ProvinceId,
                    ProvinceTitle = h.ProvinceTitle,
                    TechnicianUserId = h.TechnicianUserId,
                    TechnicianName = h.TechnicianName,
                    TechnicianSupportingDepartmentId = h.TechnicianSupportingDepartmentId,
                    TechnicianSupportingDepartmentTitle = h.TechnicianSupportingDepartmentTitle,
                    TechnicianCityId = h.TechnicianCityId,
                    TechnicianCityTitle = h.TechnicianCityTitle,
                    TechnicianProvinceId = h.TechnicianProvinceId,
                    TechnicianProvinceTitle = h.TechnicianProvinceTitle,
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

        public async Task<EquipmentTechnicianDto?> GetTechnicianAsync(long equipmentId, CancellationToken cancellationToken = default)
        {
            // an equipment that was deactivated still shows who had it
            if (!await _repository.ExistsIncludingInactiveAsync(x => x.EquipmentId == equipmentId, cancellationToken))
                throw NotFound();

            var row = await _repository.GetTechnicianOfEquipmentAsync(equipmentId, cancellationToken);
            return row is null ? null : new EquipmentTechnicianDto
            {
                Id = row.Id,
                FullName = row.FullName,
                PersonnelCode = row.PersonnelCode,
                IsActive = row.IsActive,
                PostId = row.PostId,
                PostTitle = row.PostTitle,
                SupportingDepartmentId = row.SupportingDepartmentId,
                SupportingDepartmentTitle = row.SupportingDepartmentTitle,
                CityId = row.CityId,
                CityTitle = row.CityTitle,
                ProvinceId = row.ProvinceId,
                ProvinceTitle = row.ProvinceTitle,
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
                Items = page.Items.Select(t => new TechnicianItemDto
                {
                    Id = t.Id, FullName = t.FullName, PersonnelCode = t.PersonnelCode,
                    SupportingDepartmentId = t.SupportingDepartmentId, SupportingDepartmentTitle = t.SupportingDepartmentTitle,
                    CityId = t.CityId, CityTitle = t.CityTitle, ProvinceId = t.ProvinceId, ProvinceTitle = t.ProvinceTitle,
                }).ToList(),
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
            };
        }

        // ------------------------------------------------------------------------------------------------ write

        public async Task<EquipmentDetailDto> CreateAsync(EquipmentCreateDto request, CancellationToken cancellationToken = default)
        {
            var entity = await AddEquipmentAsync(request, cancellationToken);

            _audit.Created("Equipment", entity.EquipmentId);
            return await DetailAsync(entity.EquipmentId, cancellationToken);
        }

        /// <summary>
        /// Registers an equipment together with its files, all or nothing: the equipment and every file are saved in ONE transaction, so if a
        /// file is refused (type, content, size, count) or anything else fails, nothing at all is left behind. The files arrive as a lazy
        /// sequence because they are streamed from the request body: each one is read to the end before the next is asked for.
        /// </summary>
        public async Task<EquipmentRegistrationDto> CreateWithAttachmentsAsync(EquipmentCreateDto request, IAsyncEnumerable<AttachmentUpload> files, CancellationToken cancellationToken = default)
        {
            var target = await _attachments.GetTargetAsync(AttachmentsTable, cancellationToken);

            EquipmentEntity entity;
            var stored = new List<AttachmentItemDto>();
            await using (var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken))
            {
                entity = await AddEquipmentAsync(request, cancellationToken);

                await foreach (var file in files.WithCancellation(cancellationToken))
                {
                    if (stored.Count >= AttachmentRules.MaxFilesPerRegistration)
                        throw new BadRequestException($"در هر ثبت حداکثر {AttachmentRules.MaxFilesPerRegistration} فایل مجاز است.", ErrorCodes.AttachmentsPerRequestExceeded);

                    stored.Add(await _attachments.UploadInTransactionAsync(target, entity.EquipmentId, file, cancellationToken));
                }

                await transaction.CommitAsync(cancellationToken);
            }

            // only now, when everything is committed, does anything count as created
            _audit.Created("Equipment", entity.EquipmentId);
            foreach (var item in stored)
                _attachments.AuditUploaded(item);

            var detail = await DetailAsync(entity.EquipmentId, cancellationToken);
            var registration = new EquipmentRegistrationDto();
            CopyDetail(detail, registration);
            registration.Attachments = stored;
            registration.AttachmentCount = stored.Count;
            return registration;
        }

        private static void CopyDetail(EquipmentDetailDto from, EquipmentDetailDto to)
        {
            foreach (var property in typeof(EquipmentDetailDto).GetProperties().Where(p => p.CanRead && p.CanWrite))
                property.SetValue(to, property.GetValue(from));
        }

        /// <summary>Validates and saves a new equipment with its first location history entry (no audit, no detail).</summary>
        private async Task<EquipmentEntity> AddEquipmentAsync(EquipmentCreateDto request, CancellationToken cancellationToken)
        {
            var yaasCode = request.YaasCode.Trim();
            var location = new EquipmentLocation(request.CurrentEquipmentLocationTypeId, request.CurrentOrganizationId, request.CurrentSupportingDepartmentId, request.TechnicianUserId);

            await RequireClassificationAsync(request.EquipmentStatusId, request.EquipmentTypeId, request.EquipmentModelId, mustBeActive: true, cancellationToken);
            await RequireLocationAsync(location, cancellationToken);
            await EnsureYaasCodeIsFreeAsync(yaasCode, exceptId: null, cancellationToken);

            var entity = new EquipmentEntity
            {
                Owner = request.Owner,
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
            return entity;
        }

        public async Task<IReadOnlyList<EquipmentDetailDto>> CreateBulkAsync(EquipmentBulkCreateDto request, CancellationToken cancellationToken = default)
        {
            var items = request.Items;
            var problems = new List<ApiErrorDetail>();
            var failedRows = new HashSet<int>();
            void Fail(int index, string field, string message)
            {
                problems.Add(new ApiErrorDetail { Field = $"items[{index}].{field}", Message = message });
                failedRows.Add(index);
            }

            // References are checked once per distinct id (not once per row): a missing id is "not found", a deactivated one "inactive".
            var statuses = await ResolveAsync(_statuses, items.Select(i => i.EquipmentStatusId), x => x.EquipmentStatusId, "وضعیت تجهیز", cancellationToken);
            var types = await ResolveAsync(_types, items.Where(i => i.EquipmentTypeId.HasValue).Select(i => i.EquipmentTypeId!.Value), x => x.EquipmentTypeId, "نوع تجهیز", cancellationToken);
            var models = await ResolveAsync(_models, items.Where(i => i.EquipmentModelId.HasValue).Select(i => i.EquipmentModelId!.Value), x => x.EquipmentModelId, "مدل تجهیز", cancellationToken);
            var locationTypes = await ResolveAsync(_locationTypes, items.Select(i => i.CurrentEquipmentLocationTypeId), x => x.EquipmentLocationTypeId, "نوع مکان", cancellationToken);
            var organizations = await ResolveAsync(_organizations, items.Where(i => i.CurrentOrganizationId.HasValue).Select(i => i.CurrentOrganizationId!.Value), x => x.OrganizationId, "سازمان", cancellationToken);
            var departments = await ResolveAsync(_departments, items.Where(i => i.CurrentSupportingDepartmentId.HasValue).Select(i => i.CurrentSupportingDepartmentId!.Value), x => x.SupportingDepartmentId, "دپارتمان پشتیبانی", cancellationToken);
            var technicianStates = await _repository.GetTechnicianEligibilityAsync(items.Where(i => i.TechnicianUserId.HasValue).Select(i => i.TechnicianUserId!.Value).ToList(), cancellationToken);
            var reserved = await _repository.FindReservedYaasCodesAsync(items.Select(i => i.YaasCode.Trim()).ToList(), cancellationToken);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (statuses.TryGetValue(item.EquipmentStatusId, out var statusError) && statusError is not null) Fail(i, "equipmentStatusId", statusError);
                if (item.EquipmentTypeId is { } type && types[type] is { } typeError) Fail(i, "equipmentTypeId", typeError);
                if (item.EquipmentModelId is { } model && models[model] is { } modelError) Fail(i, "equipmentModelId", modelError);
                if (locationTypes[item.CurrentEquipmentLocationTypeId] is { } locationTypeError) Fail(i, "currentEquipmentLocationTypeId", locationTypeError);
                if (item.CurrentOrganizationId is { } organization && organizations[organization] is { } organizationError) Fail(i, "currentOrganizationId", organizationError);
                if (item.CurrentSupportingDepartmentId is { } department && departments[department] is { } departmentError) Fail(i, "currentSupportingDepartmentId", departmentError);

                if (item.TechnicianUserId is { } technician)
                {
                    if (!technicianStates.TryGetValue(technician, out var eligibility)) Fail(i, "technicianUserId", "تکنسین یافت نشد.");
                    else if (eligibility == TechnicianEligibility.Inactive) Fail(i, "technicianUserId", "تکنسین انتخاب‌شده غیرفعال است.");
                    else if (eligibility == TechnicianEligibility.NotTechnician) Fail(i, "technicianUserId", NotTechnicianMessage);
                }

                if (reserved.Contains(item.YaasCode.Trim().ToLower()))
                    Fail(i, "yaasCode", "تجهیز فعال دیگری با این یاس کد وجود دارد.");

                try
                {
                    await _locationRule.ValidateAsync(ToLocation(item), cancellationToken);
                }
                catch (BadRequestException ex)
                {
                    Fail(i, "currentEquipmentLocationTypeId", ex.Message);
                }
            }

            if (problems.Count > 0)
                throw new BulkItemsException(problems, failedRows.Count);

            var entities = new List<EquipmentEntity>(items.Count);
            foreach (var item in items)
            {
                var location = ToLocation(item);
                var entity = new EquipmentEntity
                {
                    Owner = item.Owner,
                    YaasCode = item.YaasCode.Trim(),
                    SerialNumber = Normalize(item.SerialNumber),
                    AssetNumber = Normalize(item.AssetNumber),
                    EquipmentTypeId = item.EquipmentTypeId,
                    EquipmentModelId = item.EquipmentModelId,
                    EquipmentStatusId = item.EquipmentStatusId,
                    CurrentEquipmentLocationTypeId = location.LocationTypeId,
                    CurrentOrganizationId = location.OrganizationId,
                    CurrentSupportingDepartmentId = location.SupportingDepartmentId,
                    TechnicianUserId = location.TechnicianUserId,
                    Description = Normalize(item.Description),
                };
                entities.Add(entity);
                _repository.AddLocationHistory(NewHistory(entity, location, FirstLocationNote));
            }

            // One save = one transaction: all rows or none.
            await _repository.AddRangeAsync(entities, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var ids = entities.Select(e => e.EquipmentId).ToList();
            foreach (var id in ids)
                _audit.Created("Equipment", id);

            var rows = (await _repository.GetRowsAsync(ids, cancellationToken)).ToDictionary(r => r.Id);
            return ids.Select(id =>
            {
                var row = rows[id];
                var detail = Fill(new EquipmentDetailDto(), row);
                detail.ModifiedDateTime = row.ModifiedDateTime;
                detail.RowVersion = RowVersionToken.From(row.ModifiedDateTime);
                return detail;
            }).ToList();
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

            entity.Owner = request.Owner;
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
            detail.AttachmentCount = (await _attachments.CountActiveAsync(AttachmentsTable, new[] { id }, cancellationToken)).GetValueOrDefault(id);
            return detail;
        }

        private static TDto Fill<TDto>(TDto dto, EquipmentRow row) where TDto : EquipmentListItemDto
        {
            dto.Id = row.Id;
            dto.Owner = row.Owner;
            dto.OwnerTitle = row.OwnerTitle;
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
            dto.EquipmentStatusCode = row.StatusCode;
            dto.CurrentEquipmentLocationTypeId = row.LocationTypeId;
            dto.CurrentEquipmentLocationTypeTitle = row.LocationTypeTitle;
            dto.CurrentOrganizationId = row.OrganizationId;
            dto.CurrentOrganizationTitle = row.OrganizationTitle;
            dto.FieldId = row.FieldId;
            dto.FieldTitle = row.FieldTitle;
            dto.OrganizationManagementId = row.OrganizationManagementId;
            dto.OrganizationManagementTitle = row.OrganizationManagementTitle;
            dto.ZoneId = row.ZoneId;
            dto.ZoneTitle = row.ZoneTitle;
            dto.CurrentOrganizationSupportingDepartmentId = row.OrganizationSupportingDepartmentId;
            dto.CurrentOrganizationSupportingDepartmentTitle = row.OrganizationSupportingDepartmentTitle;
            dto.CurrentSupportingDepartmentId = row.SupportingDepartmentId;
            dto.CurrentSupportingDepartmentTitle = row.SupportingDepartmentTitle;
            dto.CityId = row.CityId;
            dto.CityTitle = row.CityTitle;
            dto.ProvinceId = row.ProvinceId;
            dto.ProvinceTitle = row.ProvinceTitle;
            dto.TechnicianUserId = row.TechnicianUserId;
            dto.TechnicianName = row.TechnicianName;
            dto.TechnicianIsActive = row.TechnicianIsActive;
            dto.TechnicianPostId = row.TechnicianPostId;
            dto.TechnicianPostTitle = row.TechnicianPostTitle;
            dto.TechnicianSupportingDepartmentId = row.TechnicianSupportingDepartmentId;
            dto.TechnicianSupportingDepartmentTitle = row.TechnicianSupportingDepartmentTitle;
            dto.TechnicianCityId = row.TechnicianCityId;
            dto.TechnicianCityTitle = row.TechnicianCityTitle;
            dto.TechnicianProvinceId = row.TechnicianProvinceId;
            dto.TechnicianProvinceTitle = row.TechnicianProvinceTitle;
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
                var eligibility = (await _repository.GetTechnicianEligibilityAsync(new[] { technician }, cancellationToken))
                    .GetValueOrDefault(technician, TechnicianEligibility.NotFound);

                // The post is required of a NEW assignment (create, move). Restoring a deactivated equipment (validateRule: false) only
                // needs its old technician to be active: a person who changed post meanwhile must not block the restore.
                if (eligibility == TechnicianEligibility.NotTechnician && !validateRule)
                    eligibility = TechnicianEligibility.Eligible;

                switch (eligibility)
                {
                    case TechnicianEligibility.NotFound:
                        throw new NotFoundException("تکنسین یافت نشد.", ErrorCodes.ParentNotFound);
                    case TechnicianEligibility.Inactive:
                        throw new BadRequestException("تکنسین انتخاب‌شده غیرفعال است.", ErrorCodes.ParentInactive);
                    case TechnicianEligibility.NotTechnician:
                        throw new BadRequestException(NotTechnicianMessage, ErrorCodes.TechnicianPostRequired);
                }
            }

            if (validateRule)
                await _locationRule.ValidateAsync(location, cancellationToken);
        }

        private static EquipmentLocation ToLocation(EquipmentCreateDto item) =>
            new(item.CurrentEquipmentLocationTypeId, item.CurrentOrganizationId, item.CurrentSupportingDepartmentId, item.TechnicianUserId);

        /// <summary>
        /// Checks a set of referenced ids with one query (plus one per id that is not active, to tell "missing" from "inactive").
        /// The result maps every id to its error message, or null when it can be used.
        /// </summary>
        private static async Task<Dictionary<long, string?>> ResolveAsync<TParent>(
            IRepository<TParent> repository, IEnumerable<long> ids, System.Linq.Expressions.Expression<Func<TParent, long>> key,
            string label, CancellationToken cancellationToken)
            where TParent : EMS.Domain.Entities.BaseEntity
        {
            var wanted = ids.Distinct().ToList();
            var result = new Dictionary<long, string?>();
            if (wanted.Count == 0)
                return result;

            var active = (await repository.Query().Select(key).Where(id => wanted.Contains(id)).ToListAsync(cancellationToken)).ToHashSet();
            foreach (var id in wanted)
            {
                if (active.Contains(id)) { result[id] = null; continue; }
                try
                {
                    await ParentGuard.RequireAsync(repository, id, key, label, mustBeActive: true, cancellationToken);
                    result[id] = null;
                }
                catch (AppException ex)
                {
                    result[id] = ex.Message;
                }
            }
            return result;
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
