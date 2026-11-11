using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.NoteCategory;
using EMS.Application.Interfaces.NoteCategory;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using NoteCategoryEntity = EMS.Domain.Entities.NoteCategory;

namespace EMS.Application.Services.NoteCategory
{
    /// <summary>
    /// Catalog "موضوع یادداشت". Paging, sorting, unique title, soft delete/restore, dependents check, optimistic concurrency,
    /// audit and error codes all come from CatalogService. Declare below only what is specific to this catalog.
    /// To add a parent (e.g. a Category) see EquipmentTypeService: inject its repository and use RequireParentAsync
    /// from BeforeCreateAsync / BeforeUpdateAsync. To filter the list by a column, override ApplyFilters.
    /// </summary>
    public sealed class NoteCategoryService
        : CatalogService<NoteCategoryEntity, NoteCategoryCreateDto, NoteCategoryUpdateDto, NoteCategoryDetailDto, NoteCategoryListItemDto, NoteCategoryListRequestDto>, INoteCategoryService
    {
        public NoteCategoryService(INoteCategoryRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        /// <summary>Persian name of the catalog; also used by its validators.</summary>
        public const string EntityLabel = "موضوع یادداشت";

        protected override string Label => EntityLabel;

        protected override string DependentsMessage => "این موضوع یادداشت توسط رکوردهای فعال استفاده می‌شود و قابل غیرفعال‌سازی نیست.";

        protected override Expression<Func<NoteCategoryEntity, long>> IdSelector => x => x.NoteCategoryId;

        protected override Expression<Func<NoteCategoryEntity, NoteCategoryListItemDto>> ListItem =>
            x => new NoteCategoryListItemDto
            {
                Id = x.NoteCategoryId,
                Title = x.Title,
                IsActive = x.IsActive
            };

        // sortBy values the list accepts (besides "createddatetime"; the default order is by id)
        protected override void ConfigureSorting(SortBuilder<NoteCategoryEntity> sort) =>
            sort.By("title", x => x.Title);

        protected override NoteCategoryEntity NewEntity(NoteCategoryCreateDto request, string title) =>
            new() { Title = title };

        protected override void ApplyUpdate(NoteCategoryEntity entity, NoteCategoryUpdateDto request)
        {
            // Title and IsActive are handled by CatalogService; add the other fields here.
        }

        protected override NoteCategoryDetailDto ToDetail(NoteCategoryEntity entity) =>
            new()
            {
                Id = entity.NoteCategoryId,
                Title = entity.Title,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
