using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.FailureType;
using EMS.Application.Interfaces.FailureType;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using FailureTypeEntity = EMS.Domain.Entities.FailureType;

namespace EMS.Application.Services.FailureType
{
    /// <summary>
    /// Catalog "نوع خرابی". Paging, sorting, unique title, soft delete/restore, dependents check, optimistic concurrency,
    /// audit and error codes all come from CatalogService. Declare below only what is specific to this catalog.
    /// To add a parent (e.g. a Category) see EquipmentTypeService: inject its repository and use RequireParentAsync
    /// from BeforeCreateAsync / BeforeUpdateAsync. To filter the list by a column, override ApplyFilters.
    /// </summary>
    public sealed class FailureTypeService
        : CatalogService<FailureTypeEntity, FailureTypeCreateDto, FailureTypeUpdateDto, FailureTypeDetailDto, FailureTypeListItemDto, FailureTypeListRequestDto>, IFailureTypeService
    {
        private readonly IFailureCategoryRepository _categories;
        private readonly IEquipmentTypeRepository _equipmentTypes;
        private readonly IFailureTypeRepository _failureTypes;
        private readonly IAuditEventLogger _eventLog;

        public FailureTypeService(IFailureTypeRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit, IFailureCategoryRepository categories, IEquipmentTypeRepository equipmentTypes)
            : base(repository, unitOfWork, audit)
        {
            _categories = categories;
            _equipmentTypes = equipmentTypes;
            _failureTypes = repository;
            _eventLog = audit;
        }

        /// <summary>Persian name of the catalog; also used by its validators.</summary>
        public const string EntityLabel = "نوع خرابی";

        protected override string Label => EntityLabel;

        protected override string DependentsMessage => "این نوع خرابی در گزارش خرابی فعال استفاده شده است و قابل غیرفعال‌سازی نیست.";

        protected override string DuplicateMessage => "در این دسته نوع خرابی دیگری با این عنوان وجود دارد.";

        protected override Expression<Func<FailureTypeEntity, long>> IdSelector => x => x.FailureTypeId;

        protected override Expression<Func<FailureTypeEntity, FailureTypeListItemDto>> ListItem =>
            x => new FailureTypeListItemDto
            {
                Id = x.FailureTypeId,
                FailureCategoryId = x.FailureCategoryId,
                FailureCategoryTitle = x.FailureCategory.Title,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive,
                // only active equipment types (the soft-delete filter of the equipment type applies through the link)
                EquipmentTypes = x.EquipmentTypes.OrderBy(l => l.EquipmentType.Title).ThenBy(l => l.EquipmentTypeId)
                    .Select(l => new FailureTypeEquipmentTypeDto { Id = l.EquipmentTypeId, Title = l.EquipmentType.Title }).ToList(),
            };

        // sortBy values the list accepts (besides "createddatetime"; the default order is by id)
        protected override void ConfigureSorting(SortBuilder<FailureTypeEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code).By("failurecategoryid", x => x.FailureCategoryId);

        protected override IQueryable<FailureTypeEntity> ApplyFilters(IQueryable<FailureTypeEntity> query, FailureTypeListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }

            if (request.FailureCategoryId.HasValue)
            {
                var failureCategoryId = request.FailureCategoryId.Value;
                query = query.Where(x => x.FailureCategoryId == failureCategoryId);
            }

            // the dropdown of a report: the types linked to the equipment's type, and the ones linked to no type (they apply to every equipment type)
            if (request.EquipmentTypeId.HasValue)
            {
                var equipmentTypeId = request.EquipmentTypeId.Value;
                query = query.Where(x => !x.EquipmentTypes.Any() || x.EquipmentTypes.Any(l => l.EquipmentTypeId == equipmentTypeId));
            }
            return query;
        }

        // A title only has to be unique within its category.
        protected override Expression<Func<FailureTypeEntity, bool>>? TitleScopeForCreate(FailureTypeCreateDto request) =>
            x => x.FailureCategoryId == request.FailureCategoryId;

        protected override Expression<Func<FailureTypeEntity, bool>>? TitleScopeForUpdate(FailureTypeEntity entity, FailureTypeUpdateDto request)
        {
            var failureCategoryId = request.FailureCategoryId ?? entity.FailureCategoryId;
            return x => x.FailureCategoryId == failureCategoryId;
        }

        protected override async Task BeforeCreateAsync(FailureTypeCreateDto request, CancellationToken cancellationToken)
        {
            await RequireParentAsync(_categories, request.FailureCategoryId, x => x.FailureCategoryId, "دسته‌ی خرابی", mustBeActive: true, cancellationToken);
            foreach (var equipmentTypeId in DistinctIds(request.EquipmentTypeIds))
                await RequireParentAsync(_equipmentTypes, equipmentTypeId, x => x.EquipmentTypeId, "نوع تجهیز", mustBeActive: true, cancellationToken);
        }

        /// <summary>The requested ids: distinct, in order (a repeated id counts once).</summary>
        private static List<long> DistinctIds(IEnumerable<long>? ids) => (ids ?? Enumerable.Empty<long>()).Distinct().OrderBy(id => id).ToList();

        // The links are saved with the new failure type (the foreign key is filled in by EF when the row is added).
        protected override Task OnCreatingAsync(FailureTypeCreateDto request, FailureTypeEntity entity, CancellationToken cancellationToken)
        {
            entity.EquipmentTypes = DistinctIds(request.EquipmentTypeIds).Select(id => new FailureTypeEquipmentType { EquipmentTypeId = id }).ToList();
            return Task.CompletedTask;
        }

        protected override async Task BeforeUpdateAsync(FailureTypeEntity entity, FailureTypeUpdateDto request, CancellationToken cancellationToken)
        {
            // A parent must be active when it is being changed or when this record is being re-activated.
            var reactivating = !entity.IsActive && request.IsActive;
            var failureCategoryId = request.FailureCategoryId ?? entity.FailureCategoryId;
            await RequireParentAsync(_categories, failureCategoryId, x => x.FailureCategoryId, "دسته‌ی خرابی", mustBeActive: failureCategoryId != entity.FailureCategoryId || reactivating, cancellationToken);
        }

        // null = keep the links; a list (even an empty one) is the whole new list. Only an equipment type that is being ADDED has to be active.
        protected override async Task OnUpdatingAsync(FailureTypeEntity entity, FailureTypeUpdateDto request, CancellationToken cancellationToken)
        {
            if (request.EquipmentTypeIds is null)
                return;

            var wanted = DistinctIds(request.EquipmentTypeIds);
            var current = await _failureTypes.GetEquipmentTypeIdsAsync(entity.FailureTypeId, cancellationToken);
            if (wanted.SequenceEqual(current))
                return;

            foreach (var equipmentTypeId in wanted.Except(current))
                await RequireParentAsync(_equipmentTypes, equipmentTypeId, x => x.EquipmentTypeId, "نوع تجهیز", mustBeActive: true, cancellationToken);

            await _failureTypes.ReplaceEquipmentTypesAsync(entity.FailureTypeId, wanted, cancellationToken);
            _eventLog.Operation("FailureTypeEquipmentTypesChanged", nameof(FailureTypeEntity), entity.FailureTypeId);
        }

        protected override FailureTypeEntity NewEntity(FailureTypeCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code), FailureCategoryId = request.FailureCategoryId };

        protected override void ApplyUpdate(FailureTypeEntity entity, FailureTypeUpdateDto request)
        {
            entity.FailureCategoryId = request.FailureCategoryId ?? entity.FailureCategoryId;
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override FailureTypeDetailDto ToDetail(FailureTypeEntity entity) => Map(entity, null, new());

        // The category title needs a query (the entity only carries the foreign key after a create / update); a deactivated category still has its title.
        protected override async Task<FailureTypeDetailDto> ToDetailAsync(FailureTypeEntity entity, CancellationToken cancellationToken)
        {
            var category = await _categories.GetByIdAsync(entity.FailureCategoryId, asNoTracking: true, cancellationToken);
            var equipmentTypes = (await _failureTypes.GetEquipmentTypesAsync(new[] { entity.FailureTypeId }, cancellationToken)).GetValueOrDefault(entity.FailureTypeId)
                ?.Select(t => new FailureTypeEquipmentTypeDto { Id = t.Id, Title = t.Title }).ToList() ?? new();
            return Map(entity, category?.Title, equipmentTypes);
        }

        private static FailureTypeDetailDto Map(FailureTypeEntity entity, string? categoryTitle, List<FailureTypeEquipmentTypeDto> equipmentTypes) =>
            new()
            {
                Id = entity.FailureTypeId,
                FailureCategoryId = entity.FailureCategoryId,
                FailureCategoryTitle = categoryTitle,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                EquipmentTypes = equipmentTypes,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
