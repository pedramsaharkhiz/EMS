using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.ActionEntity;
using EMS.Application.Interfaces.ActionEntity;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using ActionEntityEntity = EMS.Domain.Entities.ActionEntity;

namespace EMS.Application.Services.ActionEntity
{
    /// <summary>
    /// Catalog "اقدام". Paging, sorting, unique title, soft delete/restore, dependents check, optimistic concurrency,
    /// audit and error codes all come from CatalogService. Declare below only what is specific to this catalog.
    /// To add a parent (e.g. a Category) see EquipmentTypeService: inject its repository and use RequireParentAsync
    /// from BeforeCreateAsync / BeforeUpdateAsync. To filter the list by a column, override ApplyFilters.
    /// </summary>
    public sealed class ActionEntityService
        : CatalogService<ActionEntityEntity, ActionEntityCreateDto, ActionEntityUpdateDto, ActionEntityDetailDto, ActionEntityListItemDto, ActionEntityListRequestDto>, IActionEntityService
    {
        public ActionEntityService(IActionEntityRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        /// <summary>Persian name of the catalog; also used by its validators.</summary>
        public const string EntityLabel = "اقدام";

        protected override string Label => EntityLabel;

        // TODO: say what depends on this catalog (and implement ActionEntityRepository.HasActiveDependentsAsync to match).
        protected override string DependentsMessage => "این اقدام در گزارش یا ارجاع فعال استفاده شده است و قابل غیرفعال‌سازی نیست.";

        protected override Expression<Func<ActionEntityEntity, long>> IdSelector => x => x.ActionId;

        protected override Expression<Func<ActionEntityEntity, ActionEntityListItemDto>> ListItem =>
            x => new ActionEntityListItemDto
            {
                Id = x.ActionId,
                Title = x.Title,
                Code = x.Code,
                Description = x.Description,
                IsActive = x.IsActive
            };

        // sortBy values the list accepts (besides "createddatetime"; the default order is by id)
        protected override void ConfigureSorting(SortBuilder<ActionEntityEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code);

        protected override IQueryable<ActionEntityEntity> ApplyFilters(IQueryable<ActionEntityEntity> query, ActionEntityListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override ActionEntityEntity NewEntity(ActionEntityCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code), Description = Normalize(request.Description) };

        protected override void ApplyUpdate(ActionEntityEntity entity, ActionEntityUpdateDto request)
        {
            entity.Code = Keep(request.Code, entity.Code);
            entity.Description = Keep(request.Description, entity.Description);
        }

        protected override ActionEntityDetailDto ToDetail(ActionEntityEntity entity) =>
            new()
            {
                Id = entity.ActionId,
                Title = entity.Title,
                Code = entity.Code,
                Description = entity.Description,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
