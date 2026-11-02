using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Widget;
using EMS.Application.Interfaces.Widget;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using WidgetEntity = EMS.Domain.Entities.Widget;

namespace EMS.Application.Services.Widget
{
    /// <summary>
    /// Catalog "__LABEL__". Paging, sorting, unique title, soft delete/restore, dependents check, optimistic concurrency,
    /// audit and error codes all come from CatalogService. Declare below only what is specific to this catalog.
    /// To add a parent (e.g. a Category) see EquipmentTypeService: inject its repository and use RequireParentAsync
    /// from BeforeCreateAsync / BeforeUpdateAsync. To filter the list by a column, override ApplyFilters.
    /// </summary>
    public sealed class WidgetService
        : CatalogService<WidgetEntity, WidgetCreateDto, WidgetUpdateDto, WidgetDetailDto, WidgetListItemDto, WidgetListRequestDto>, IWidgetService
    {
        public WidgetService(IWidgetRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        /// <summary>Persian name of the catalog; also used by its validators.</summary>
        public const string EntityLabel = "__LABEL__";

        protected override string Label => EntityLabel;

        // TODO: say what depends on this catalog (and implement WidgetRepository.HasActiveDependentsAsync to match).
        protected override string DependentsMessage => "این __LABEL__ توسط رکوردهای فعال استفاده می‌شود و قابل غیرفعال‌سازی نیست.";

        protected override Expression<Func<WidgetEntity, long>> IdSelector => x => x.WidgetId;

        protected override Expression<Func<WidgetEntity, WidgetListItemDto>> ListItem =>
            x => new WidgetListItemDto
            {
                WidgetId = x.WidgetId,
                Title = x.Title,
//#if (HasCode)
                Code = x.Code!,
//#endif
                IsActive = x.IsActive
            };

        // sortBy values the list accepts (besides "createddatetime"; the default order is by id)
        protected override void ConfigureSorting(SortBuilder<WidgetEntity> sort) =>
//#if (HasCode)
            sort.By("title", x => x.Title).By("code", x => x.Code);
//#else
            sort.By("title", x => x.Title);
//#endif
//#if (HasCode)

        protected override IQueryable<WidgetEntity> ApplyFilters(IQueryable<WidgetEntity> query, WidgetListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }
//#endif

        protected override WidgetEntity NewEntity(WidgetCreateDto request, string title) =>
//#if (HasCode)
            new() { Title = title, Code = Normalize(request.Code) };
//#else
            new() { Title = title };
//#endif

        protected override void ApplyUpdate(WidgetEntity entity, WidgetUpdateDto request)
        {
//#if (HasCode)
            entity.Code = Keep(request.Code, entity.Code);
//#else
            // Title and IsActive are handled by CatalogService; add the other fields here.
//#endif
        }

        protected override WidgetDetailDto ToDetail(WidgetEntity entity) =>
            new()
            {
                WidgetId = entity.WidgetId,
                Title = entity.Title,
//#if (HasCode)
                Code = entity.Code!,
//#endif
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
