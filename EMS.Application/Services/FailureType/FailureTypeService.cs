using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.FailureType;
using EMS.Application.Interfaces.FailureType;
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

        public FailureTypeService(IFailureTypeRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit, IFailureCategoryRepository categories)
            : base(repository, unitOfWork, audit)
        {
            _categories = categories;
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
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive
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
        }

        protected override async Task BeforeUpdateAsync(FailureTypeEntity entity, FailureTypeUpdateDto request, CancellationToken cancellationToken)
        {
            // A parent must be active when it is being changed or when this record is being re-activated.
            var reactivating = !entity.IsActive && request.IsActive;
            var failureCategoryId = request.FailureCategoryId ?? entity.FailureCategoryId;
            await RequireParentAsync(_categories, failureCategoryId, x => x.FailureCategoryId, "دسته‌ی خرابی", mustBeActive: failureCategoryId != entity.FailureCategoryId || reactivating, cancellationToken);
        }

        protected override FailureTypeEntity NewEntity(FailureTypeCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code), FailureCategoryId = request.FailureCategoryId };

        protected override void ApplyUpdate(FailureTypeEntity entity, FailureTypeUpdateDto request)
        {
            entity.FailureCategoryId = request.FailureCategoryId ?? entity.FailureCategoryId;
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override FailureTypeDetailDto ToDetail(FailureTypeEntity entity) =>
            new()
            {
                Id = entity.FailureTypeId,
                FailureCategoryId = entity.FailureCategoryId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
