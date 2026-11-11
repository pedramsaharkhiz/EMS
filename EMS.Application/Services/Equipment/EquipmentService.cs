using EMS.Application.Common.Catalog;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Export;
using EMS.Application.Common.Filtering;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Common.People;
using EMS.Application.Dtos.Attachment;
using EMS.Application.Interfaces.Attachment;
using EMS.Application.Services.Attachment;
using EMS.Application.Dtos.Dashboard;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Dtos.EquipmentLocationHistory;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.Equipment;
using EMS.Domain;
using EMS.Domain.Enums;
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
            .AnyOf(r => r.LocationKinds, x => x.LocationKind)
            .AnyOf(r => r.OrganizationIds, x => x.OrganizationId)
            .AnyOf(r => r.FieldIds, x => x.FieldId)
            .AnyOf(r => r.OrganizationManagementIds, x => x.OrganizationManagementId)
            .AnyOf(r => r.ZoneIds, x => x.ZoneId)
            .AnyOf(r => r.SupportingDepartmentIds, x => x.OrganizationSupportingDepartmentId)
            .AnyOf(r => r.CurrentSupportingDepartmentIds, x => x.SupportingDepartmentId)
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
            .By("locationkind", x => x.LocationKindTitle)
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
        private readonly ISupportingDepartmentRepository _departments;
        private readonly IRepository<OrganizationEntity> _organizations;
        private readonly IEquipmentLocationRule _locationRule;
        private readonly IExcelExporter _exporter;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;
        private readonly IAttachmentService _attachments;
        private readonly IUserReferenceRepository _people;

        public EquipmentService(
            IEquipmentRepository repository,
            IEquipmentStatusRepository statuses,
            IEquipmentTypeRepository types,
            IEquipmentModelRepository models,
            ISupportingDepartmentRepository departments,
            IRepository<OrganizationEntity> organizations,
            IEquipmentLocationRule locationRule,
            IExcelExporter exporter,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            IAuditEventLogger audit,
            IAttachmentService attachments,
            IUserReferenceRepository people)
        {
            _repository = repository;
            _statuses = statuses;
            _types = types;
            _models = models;
            _departments = departments;
            _organizations = organizations;
            _locationRule = locationRule;
            _exporter = exporter;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _audit = audit;
            _attachments = attachments;
            _people = people;
        }

        // ------------------------------------------------------------------------------------------------ read

        public async Task<PagedResult<EquipmentListItemDto>> GetOfUserAsync(long userId, EquipmentListRequestDto request, CancellationToken cancellationToken = default)
        {
            // an unknown user is an error, not an empty list (a user who holds nothing is an empty list); active or not, he exists
            if (!(await _repository.FindExistingUserIdsAsync(new[] { userId }, cancellationToken)).Contains(userId))
                throw new NotFoundException("کاربر موردنظر پیدا نشد.", ErrorCodes.UserNotFound);

            // the user of the route decides: a technicianUserId sent in the query is replaced, every other list filter works as in the list
            request.TechnicianUserIds = new[] { userId };
            return await GetPagedAsync(request, cancellationToken);
        }

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

        public async Task<EquipmentFleetSummaryDto> GetFleetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var rows = VisibleRows(true);
            var byStatus = (await rows.GroupBy(r => r.StatusId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync(cancellationToken))
                .ToDictionary(x => x.Id, x => x.Count);
            var byOwner = (await rows.GroupBy(r => r.Owner).Select(g => new { Owner = g.Key, Count = g.Count() }).ToListAsync(cancellationToken))
                .ToDictionary(x => x.Owner, x => x.Count);

            // every active status row (so an empty bar can be drawn) plus a deactivated one that still holds equipment
            var statuses = await _statuses.QueryByActive(true).OrderBy(x => x.EquipmentStatusId).ToListAsync(cancellationToken);
            var inactiveWithEquipment = byStatus.Keys.Except(statuses.Select(x => x.EquipmentStatusId)).ToList();
            if (inactiveWithEquipment.Count > 0)
                statuses.AddRange(await _statuses.QueryByActive(false).Where(x => inactiveWithEquipment.Contains(x.EquipmentStatusId)).ToListAsync(cancellationToken));

            var bars = statuses.OrderBy(x => x.EquipmentStatusId).Select(x => new EquipmentStatusCountDto
            {
                Id = x.EquipmentStatusId,
                Code = x.Code,
                Title = x.Title,
                Count = byStatus.GetValueOrDefault(x.EquipmentStatusId),
                NeedsAction = EquipmentStatusCatalog.NeedsActionIds.Contains(x.EquipmentStatusId),
            }).ToList();

            var owners = new List<DashboardOwnerCountDto>();
            foreach (var owner in new[] { EquipmentOwner.Bank, EquipmentOwner.Company })
                owners.Add(new DashboardOwnerCountDto { Owner = (int)owner, Title = EquipmentOwnerTitles.TitleOf(owner), Count = byOwner.GetValueOrDefault(owner) });
            foreach (var (owner, count) in byOwner.Where(x => x.Key != EquipmentOwner.Bank && x.Key != EquipmentOwner.Company && x.Value > 0))
                owners.Add(new DashboardOwnerCountDto { Owner = (int)owner, Title = EquipmentOwnerTitles.TitleOf(owner), Count = count });

            return new EquipmentFleetSummaryDto
            {
                Total = byStatus.Values.Sum(),
                NeedsActionCount = bars.Where(b => b.NeedsAction).Sum(b => b.Count),
                ByStatus = bars,
                ByOwner = owners,
            };
        }

        public async Task<ExcelFile> ExportAsync(EquipmentListRequestDto request, CancellationToken cancellationToken = default) =>
            await _exporter.ExportAsync(Query(request), EquipmentExport.Definition, cancellationToken);

        public async Task<PagedResult<EquipmentLocationHistoryItemDto>> GetLocationHistoryAsync(long id, EquipmentLocationHistoryRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsIncludingInactiveAsync(x => x.EquipmentId == id, cancellationToken))
                throw NotFound();

            var page = await _repository.QueryLocationHistory(id).ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

            // the technician and the one who made each change, all in one query
            var people = await _people.GetPeopleAsync(page.Items.SelectMany(h => new[] { h.TechnicianUserId, h.ChangedByUserId }), cancellationToken);
            var items = page.Items.Select(h =>
            {
                var item = new EquipmentLocationHistoryItemDto
                {
                    Id = h.Id,
                    LocationKind = h.LocationKind,
                    LocationKindTitle = h.LocationKindTitle,
                    CurrentOrganizationId = h.OrganizationId,
                    CurrentOrganizationTitle = h.OrganizationTitle,
                    CurrentSupportingDepartmentId = h.SupportingDepartmentId,
                    CurrentSupportingDepartmentTitle = h.SupportingDepartmentTitle,
                    CurrentCityId = h.CityId,
                    CurrentCityTitle = h.CityTitle,
                    CurrentProvinceId = h.ProvinceId,
                    CurrentProvinceTitle = h.ProvinceTitle,
                    TechnicianUserId = h.TechnicianUserId,
                    AssignedDateTime = h.AssignedDateTime,
                    ChangedByUserId = h.ChangedByUserId,
                    Description = h.Description,
                };
                PersonReferenceMapper.Fill(item, "Technician", h.TechnicianUserId is { } t ? people.GetValueOrDefault(t) : null);
                PersonReferenceMapper.Fill(item, "ChangedBy", h.ChangedByUserId is { } c ? people.GetValueOrDefault(c) : null);
                return item;
            }).ToList();

            return new PagedResult<EquipmentLocationHistoryItemDto>
            {
                Items = items,
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
            var location = new EquipmentLocation(request.CurrentOrganizationId, request.CurrentSupportingDepartmentId, request.TechnicianUserId);

            await RequireClassificationAsync(request.EquipmentStatusId, request.EquipmentTypeId, request.EquipmentModelId, mustBeActive: true, cancellationToken);
            await RequireLocationAsync(location, cancellationToken);
            await EnsureSerialNumberIsFreeAsync(Normalize(request.SerialNumber), exceptId: null, cancellationToken);
            await EnsureAssetNumberIsFreeAsync(Normalize(request.AssetNumber), exceptId: null, cancellationToken);

            var entity = new EquipmentEntity
            {
                Owner = request.Owner,
                YaasCode = yaasCode,
                SerialNumber = Normalize(request.SerialNumber),
                AssetNumber = Normalize(request.AssetNumber),
                EquipmentTypeId = request.EquipmentTypeId,
                EquipmentModelId = request.EquipmentModelId,
                EquipmentStatusId = request.EquipmentStatusId,
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
            var organizations = await ResolveAsync(_organizations, items.Where(i => i.CurrentOrganizationId.HasValue).Select(i => i.CurrentOrganizationId!.Value), x => x.OrganizationId, "شعبه", cancellationToken);
            var departments = await ResolveAsync(_departments, items.Where(i => i.CurrentSupportingDepartmentId.HasValue).Select(i => i.CurrentSupportingDepartmentId!.Value), x => x.SupportingDepartmentId, "دپارتمان پشتیبانی", cancellationToken);
            var existingTechnicians = await _repository.FindExistingUserIdsAsync(items.Where(i => i.TechnicianUserId.HasValue).Select(i => i.TechnicianUserId!.Value).ToList(), cancellationToken);
            var takenSerials = await _repository.FindExistingSerialNumbersAsync(items.Select(i => Normalize(i.SerialNumber)).OfType<string>().ToList(), cancellationToken);
            var takenAssets = await _repository.FindExistingAssetNumbersAsync(items.Select(i => Normalize(i.AssetNumber)).OfType<string>().ToList(), cancellationToken);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (statuses.TryGetValue(item.EquipmentStatusId, out var statusError) && statusError is not null) Fail(i, "equipmentStatusId", statusError);
                if (item.EquipmentTypeId is { } type && types[type] is { } typeError) Fail(i, "equipmentTypeId", typeError);
                if (item.EquipmentModelId is { } model && models[model] is { } modelError) Fail(i, "equipmentModelId", modelError);
                if (!ToLocation(item).HasExactlyOneHolder) Fail(i, "currentOrganizationId", EquipmentLocation.HolderMessage);
                if (item.CurrentOrganizationId is { } organization && organizations[organization] is { } organizationError) Fail(i, "currentOrganizationId", organizationError);
                if (item.CurrentSupportingDepartmentId is { } department && departments[department] is { } departmentError) Fail(i, "currentSupportingDepartmentId", departmentError);

                // a technician is any user: only that he exists is checked (neither his post nor whether he is active)
                if (item.TechnicianUserId is { } technician && !existingTechnicians.Contains(technician))
                    Fail(i, "technicianUserId", "تکنسین یافت نشد.");

                if (Normalize(item.SerialNumber) is { } serial && takenSerials.Contains(serial.ToLower()))
                    Fail(i, "serialNumber", SerialTakenMessage);
                if (Normalize(item.AssetNumber) is { } asset && takenAssets.Contains(asset.ToLower()))
                    Fail(i, "assetNumber", AssetTakenMessage);

                try
                {
                    await _locationRule.ValidateAsync(ToLocation(item), cancellationToken);
                }
                catch (BadRequestException ex)
                {
                    Fail(i, "currentOrganizationId", ex.Message);
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

            await _locationRule.ValidateAsync(new EquipmentLocation(entity.CurrentOrganizationId, entity.CurrentSupportingDepartmentId, entity.TechnicianUserId), cancellationToken);
            var yaasCode = request.YaasCode.Trim();
            var reactivating = !entity.IsActive && request.IsActive;
            var statusChanged = entity.EquipmentStatusId != request.EquipmentStatusId;

            // References must be active when the record is (and still will be) active and the reference is new or the record is
            // being restored; for a record that stays deactivated they only have to exist.
            var checkActive = request.IsActive && (reactivating || statusChanged
                || entity.EquipmentTypeId != request.EquipmentTypeId || entity.EquipmentModelId != request.EquipmentModelId);
            await RequireClassificationAsync(request.EquipmentStatusId, request.EquipmentTypeId, request.EquipmentModelId, checkActive, cancellationToken);

            if (reactivating)
                await RequireLocationAsync(new EquipmentLocation(entity.CurrentOrganizationId, entity.CurrentSupportingDepartmentId, entity.TechnicianUserId), cancellationToken, validateRule: false);

            if (statusChanged && !EquipmentStatusTransitions.IsAllowed(entity.EquipmentStatusId, request.EquipmentStatusId))
                throw new BadRequestException("تغییر وضعیت تجهیز به وضعیت انتخاب‌شده مجاز نیست.", ErrorCodes.StatusTransitionNotAllowed);

            // Serial and asset numbers are unique among ALL equipment: only a changed value can collide (restoring keeps the equipment's own).
            var serial = Normalize(request.SerialNumber);
            var asset = Normalize(request.AssetNumber);
            if (!string.Equals(entity.SerialNumber, serial, StringComparison.OrdinalIgnoreCase))
                await EnsureSerialNumberIsFreeAsync(serial, id, cancellationToken);
            if (!string.Equals(entity.AssetNumber, asset, StringComparison.OrdinalIgnoreCase))
                await EnsureAssetNumberIsFreeAsync(asset, id, cancellationToken);

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

        /// <summary>
        /// A partial move: what the request does not name stays as it is. The result is the current state with the named changes applied; the
        /// branch and the department exclude each other, so naming one leaves the other. Only that the branch, the department and the technician
        /// EXIST is checked here (a missing one would be a database error): whether they are active, or what post the technician has, is not
        /// restricted (see docs/adr/0015-partial-equipment-move.md).
        /// </summary>
        public async Task<EquipmentDetailDto> MoveAsync(long id, EquipmentMoveDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound();

            var location = TargetOf(entity, request.CurrentOrganizationId, request.CurrentSupportingDepartmentId, request.TechnicianUserId, request.ClearTechnician);

            // The validator refuses these; this keeps the rule even for a caller that skips it (the database constraint is the last guard).
            if (request.CurrentOrganizationId.HasValue && request.CurrentSupportingDepartmentId.HasValue || !location.HasExactlyOneHolder)
                throw new BadRequestException(EquipmentLocation.HolderMessage, ErrorCodes.LocationHolderInvalid);

            // Asking for what already is changes nothing and adds no history entry.
            if (location == new EquipmentLocation(entity.CurrentOrganizationId, entity.CurrentSupportingDepartmentId, entity.TechnicianUserId))
                return await DetailAsync(id, cancellationToken);

            await RequireMoveTargetsExistAsync(request.CurrentOrganizationId, request.CurrentSupportingDepartmentId, request.TechnicianUserId, cancellationToken);
            await _locationRule.ValidateAsync(location, cancellationToken);

            entity.CurrentOrganizationId = location.OrganizationId;
            entity.CurrentSupportingDepartmentId = location.SupportingDepartmentId;
            entity.TechnicianUserId = location.TechnicianUserId;

            ExpectVersion(entity, request.RowVersion);
            _repository.Update(entity);
            _repository.AddLocationHistory(NewHistory(entity, location, Normalize(request.Description)));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Operation("LocationChanged", "Equipment", id,
                $"organization={location.OrganizationId};department={location.SupportingDepartmentId};technician={location.TechnicianUserId}");
            return await DetailAsync(id, cancellationToken);
        }

        /// <summary>
        /// The state an equipment is in after a (partial) move: what was not named stays, naming a branch leaves the department and the other way
        /// round, and <paramref name="clearTechnician"/> takes the technician off.
        /// </summary>
        private static EquipmentLocation TargetOf(EquipmentEntity entity, long? organizationId, long? departmentId, long? technicianId, bool clearTechnician) => new(
            OrganizationId: organizationId ?? (departmentId.HasValue ? null : entity.CurrentOrganizationId),
            SupportingDepartmentId: departmentId ?? (organizationId.HasValue ? null : entity.CurrentSupportingDepartmentId),
            TechnicianUserId: clearTechnician ? null : technicianId ?? entity.TechnicianUserId);

        /// <summary>
        /// The same change for many equipment, all or nothing in one transaction. Each equipment ends in the state a single <c>move</c> would give
        /// it; the ones already in that state are counted as unchanged and get no history entry.
        /// </summary>
        public async Task<EquipmentBulkMoveResultDto> BulkMoveAsync(EquipmentBulkMoveDto request, CancellationToken cancellationToken = default)
        {
            var ids = request.EquipmentIds ?? new List<long>();
            var entities = (await _repository.GetManyForUpdateAsync(ids, cancellationToken)).ToDictionary(e => e.EquipmentId);

            var problems = new List<ApiErrorDetail>();
            var failedIds = new HashSet<long>();
            for (var i = 0; i < ids.Count; i++)
            {
                if (!entities.ContainsKey(ids[i]))
                {
                    problems.Add(new ApiErrorDetail { Field = $"equipmentIds[{i}]", Message = "تجهیز موردنظر پیدا نشد." });
                    failedIds.Add(ids[i]);
                }
            }
            if (problems.Count > 0)
                throw new BulkItemsException(problems, $"جابه‌جایی گروهی انجام نشد؛ {failedIds.Count} تجهیز پیدا نشد و هیچ تجهیزی جابه‌جا نشد.");

            var changes = new List<(EquipmentEntity Entity, EquipmentLocation Location)>();
            foreach (var id in ids)
            {
                var entity = entities[id];
                var location = TargetOf(entity, request.CurrentOrganizationId, request.CurrentSupportingDepartmentId, request.TechnicianUserId, request.ClearTechnician);
                if (!location.HasExactlyOneHolder)
                    throw new BadRequestException(EquipmentLocation.HolderMessage, ErrorCodes.LocationHolderInvalid);
                if (location != new EquipmentLocation(entity.CurrentOrganizationId, entity.CurrentSupportingDepartmentId, entity.TechnicianUserId))
                    changes.Add((entity, location));
            }

            if (changes.Count > 0)
            {
                // the destination is the same for all: its existence is checked once
                await RequireMoveTargetsExistAsync(request.CurrentOrganizationId, request.CurrentSupportingDepartmentId, request.TechnicianUserId, cancellationToken);

                var rule = new List<ApiErrorDetail>();
                for (var i = 0; i < ids.Count; i++)
                {
                    var change = changes.FirstOrDefault(c => c.Entity.EquipmentId == ids[i]);
                    if (change.Entity is null) continue;
                    try { await _locationRule.ValidateAsync(change.Location, cancellationToken); }
                    catch (BadRequestException ex) { rule.Add(new ApiErrorDetail { Field = $"equipmentIds[{i}]", Message = ex.Message }); }
                }
                if (rule.Count > 0)
                    throw new BulkItemsException(rule, $"جابه‌جایی گروهی انجام نشد؛ {rule.Count} تجهیز شرایط را ندارد و هیچ تجهیزی جابه‌جا نشد.");

                var description = Normalize(request.Description);
                foreach (var (entity, location) in changes)
                {
                    entity.CurrentOrganizationId = location.OrganizationId;
                    entity.CurrentSupportingDepartmentId = location.SupportingDepartmentId;
                    entity.TechnicianUserId = location.TechnicianUserId;
                    _repository.Update(entity);
                    _repository.AddLocationHistory(NewHistory(entity, location, description));
                }

                // one save = one transaction: all the equipment or none (a concurrent change of any of them is a 409 for the whole request)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var (entity, location) in changes)
                    _audit.Operation("LocationChanged", "Equipment", entity.EquipmentId,
                        $"organization={location.OrganizationId};department={location.SupportingDepartmentId};technician={location.TechnicianUserId}");
                _audit.Operation("BulkMoved", "Equipment", null, $"selected={ids.Count};changed={changes.Count}");
            }

            return new EquipmentBulkMoveResultDto
            {
                Selected = ids.Count,
                Changed = changes.Count,
                Unchanged = ids.Count - changes.Count,
                ChangedIds = changes.Select(c => c.Entity.EquipmentId).ToList(),
            };
        }

        /// <summary>The named branch, department and technician must exist (404). Active or not, and the technician's post, are not checked.</summary>
        private async Task RequireMoveTargetsExistAsync(long? organizationId, long? departmentId, long? technicianId, CancellationToken cancellationToken)
        {
            if (organizationId is { } organization)
                await ParentGuard.RequireAsync(_organizations, organization, x => x.OrganizationId, "شعبه", mustBeActive: false, cancellationToken);
            if (departmentId is { } department)
                await ParentGuard.RequireAsync(_departments, department, x => x.SupportingDepartmentId, "دپارتمان پشتیبانی", mustBeActive: false, cancellationToken);
            if (technicianId is { } technician)
            {
                if (!(await _repository.FindExistingUserIdsAsync(new[] { technician }, cancellationToken)).Contains(technician))
                    throw new NotFoundException("تکنسین یافت نشد.", ErrorCodes.ParentNotFound);
            }
        }

        // ------------------------------------------------------------------------------------------------ helpers

        /// <summary>
        /// THE source of equipment rows for the list, the Excel export AND the dashboard counts. Whatever decides which equipment a caller may see
        /// (a future row-level security rule) belongs here, once. Do not query <see cref="IEquipmentRepository.QueryRows"/> from anywhere else in this service.
        /// </summary>
        private IQueryable<EquipmentRow> VisibleRows(bool active) => _repository.QueryRows(active);

        private IQueryable<EquipmentRow> Query(EquipmentListRequestDto request)
        {
            var rows = Filters.Apply(VisibleRows(request.IsActive ?? true), request);

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
            dto.LocationKind = row.LocationKind;
            dto.LocationKindTitle = row.LocationKindTitle;
            dto.CurrentOrganizationId = row.OrganizationId;
            dto.CurrentOrganizationTitle = row.OrganizationTitle;
            dto.FieldId = row.FieldId;
            dto.FieldTitle = row.FieldTitle;
            dto.OrganizationManagementId = row.OrganizationManagementId;
            dto.OrganizationManagementTitle = row.OrganizationManagementTitle;
            dto.ZoneId = row.ZoneId;
            dto.ZoneTitle = row.ZoneTitle;
            dto.SupportingDepartmentId = row.OrganizationSupportingDepartmentId;
            dto.SupportingDepartmentTitle = row.OrganizationSupportingDepartmentTitle;
            dto.CurrentSupportingDepartmentId = row.SupportingDepartmentId;
            dto.CurrentSupportingDepartmentTitle = row.SupportingDepartmentTitle;
            dto.CurrentCityId = row.CityId;
            dto.CurrentCityTitle = row.CityTitle;
            dto.CurrentProvinceId = row.ProvinceId;
            dto.CurrentProvinceTitle = row.ProvinceTitle;
            dto.TechnicianUserId = row.TechnicianUserId;
            dto.TechnicianName = row.TechnicianName;
            dto.TechnicianPersonnelCode = row.TechnicianPersonnelCode;
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
            if (validateRule && !location.HasExactlyOneHolder)
                throw new BadRequestException(EquipmentLocation.HolderMessage, ErrorCodes.LocationHolderInvalid);

            if (location.OrganizationId is { } organization)
                await ParentGuard.RequireAsync(_organizations, organization, x => x.OrganizationId, "شعبه", mustBeActive: true, cancellationToken);
            if (location.SupportingDepartmentId is { } department)
                await ParentGuard.RequireAsync(_departments, department, x => x.SupportingDepartmentId, "دپارتمان پشتیبانی", mustBeActive: true, cancellationToken);

            // a technician is any user: only that he exists is checked (neither his post nor whether he is active), as in a move
            if (location.TechnicianUserId is { } technician
                && !(await _repository.FindExistingUserIdsAsync(new[] { technician }, cancellationToken)).Contains(technician))
                throw new NotFoundException("تکنسین یافت نشد.", ErrorCodes.ParentNotFound);

            if (validateRule)
                await _locationRule.ValidateAsync(location, cancellationToken);
        }

        private static EquipmentLocation ToLocation(EquipmentCreateDto item) =>
            new(item.CurrentOrganizationId, item.CurrentSupportingDepartmentId, item.TechnicianUserId);

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

        private const string SerialTakenMessage = EquipmentMessages.SerialTaken;
        private const string AssetTakenMessage = EquipmentMessages.AssetTaken;

        // null / blank means "none": any number of equipment may have no serial number, only a given one must be unique.
        private async Task EnsureSerialNumberIsFreeAsync(string? serialNumber, long? exceptId, CancellationToken cancellationToken)
        {
            if (serialNumber is not null && await _repository.SerialNumberExistsAsync(serialNumber, exceptId, cancellationToken))
                throw new ConflictException(SerialTakenMessage, ErrorCodes.DuplicateSerialNumber);
        }

        private async Task EnsureAssetNumberIsFreeAsync(string? assetNumber, long? exceptId, CancellationToken cancellationToken)
        {
            if (assetNumber is not null && await _repository.AssetNumberExistsAsync(assetNumber, exceptId, cancellationToken))
                throw new ConflictException(AssetTakenMessage, ErrorCodes.DuplicateAssetNumber);
        }

        private EquipmentLocationHistoryEntity NewHistory(EquipmentEntity equipment, EquipmentLocation location, string? description) =>
            new()
            {
                Equipment = equipment,
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

        /// <summary>Returns active equipment currently located at the requested organization using the shared equipment filters and paging.</summary>
        public async Task<PagedResult<OrganizationEquipmentListItemDto>> GetOfOrganizationAsync(OrganizationEquipmentListRequestDto request, CancellationToken cancellationToken = default)
        {
            var organizationExists = await _organizations.ExistsIncludingInactiveAsync(x => x.OrganizationId == request.OrganizationId, cancellationToken);

            if (!organizationExists)
                throw new NotFoundException("سازمان موردنظر پیدا نشد.", ErrorCodes.NotFound);

            var filters = new EquipmentListRequestDto
            {
                OrganizationIds = new[] { request.OrganizationId },
                Search = request.Search?.Trim(),
                IsActive = true,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            };

            var query = Query(filters).Select(x => new OrganizationEquipmentListItemDto
            {
                Id = x.Id,
                YaasCode = x.YaasCode,
                SerialNumber = x.SerialNumber,
                AssetNumber = x.AssetNumber
            });

            return await query.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

    }
}
