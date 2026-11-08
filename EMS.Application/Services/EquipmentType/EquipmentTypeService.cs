using EMS.Application.Common.Exceptions;
using EMS.Application.Extensions;
using EMS.Domain.ReadModels;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Interfaces.EquipmentType;
using EMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using EquipmentTypeEntity = EMS.Domain.Entities.EquipmentType;

namespace EMS.Application.Services.EquipmentType
{
    /// <summary>Catalog "نوع تجهیز". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class EquipmentTypeService
        : CatalogService<EquipmentTypeEntity, EquipmentTypeCreateDto, EquipmentTypeUpdateDto, EquipmentTypeDetailDto, EquipmentTypeListItemDto, EquipmentTypeListRequestDto>, IEquipmentTypeService
    {
        private readonly IEquipmentCategoryRepository _categories;
        private readonly IEquipmentTypeRepository _types;
        public EquipmentTypeService(IEquipmentTypeRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit, IEquipmentCategoryRepository categories)
            : base(repository, unitOfWork, audit)
        {
            _categories = categories;
            _types = repository;
        }

        // sortBy values of the "types without models" list
        private static readonly SortBuilder<EquipmentTypeWithCategoryRow> WithoutModelsSorting = new SortBuilder<EquipmentTypeWithCategoryRow>()
            .By("title", x => x.Title).By("code", x => x.Code).By("category", x => x.EquipmentCategoryTitle);

        public async Task<PagedResult<EquipmentTypeListItemDto>> GetWithoutModelsAsync(EquipmentTypeWithoutModelsRequestDto request, CancellationToken cancellationToken = default)
        {
            var query = _types.QueryWithoutModels();
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                var title = request.Title.Trim();
                query = query.Where(x => x.Title.Contains(title));
            }
            if (request.EquipmentCategoryId.HasValue)
            {
                var categoryId = request.EquipmentCategoryId.Value;
                query = query.Where(x => x.EquipmentCategoryId == categoryId);
            }

            var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            if (!WithoutModelsSorting.TryApply(request.SortBy, descending, query, out var sorted))
                sorted = descending ? query.OrderByDescending(x => x.Title).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Title).ThenBy(x => x.Id);

            var page = await sorted.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
            return new PagedResult<EquipmentTypeListItemDto>
            {
                Items = page.Items.Select(ToDto).ToList(),
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
            };
        }

        public async Task<EquipmentTypeListItemDto> GetByModelIdAsync(long modelId, CancellationToken cancellationToken = default)
        {
            if (modelId <= 0)
                throw new FluentValidation.ValidationException(new[] { new FluentValidation.Results.ValidationFailure("modelId", "شناسه مدل تجهیز باید بزرگتر از صفر باشد.") });

            var row = await _types.QueryTypeOfModel(modelId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("مدل تجهیز موردنظر پیدا نشد.", ErrorCodes.NotFound);
            return ToDto(row);
        }

        private static EquipmentTypeListItemDto ToDto(EquipmentTypeWithCategoryRow row) => new()
        {
            Id = row.Id, Title = row.Title, Code = row.Code, IsActive = row.IsActive,
            EquipmentCategoryId = row.EquipmentCategoryId, EquipmentCategoryTitle = row.EquipmentCategoryTitle, HasModels = row.HasModels,
        };

        public const string EntityLabel = "نوع تجهیز";
        protected override string Label => EntityLabel;
        protected override string DependentsMessage => "این نوع تجهیز دارای مدل یا تجهیز فعال است و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<EquipmentTypeEntity, long>> IdSelector => x => x.EquipmentTypeId;

        protected override Expression<Func<EquipmentTypeEntity, EquipmentTypeListItemDto>> ListItem =>
            x => new EquipmentTypeListItemDto { Id = x.EquipmentTypeId, EquipmentCategoryId = x.EquipmentCategoryId, EquipmentCategoryTitle = x.EquipmentCategory.Title, Title = x.Title, Code = x.Code, IsActive = x.IsActive,
                // the models are a catalog with the soft-delete filter, and a list of deactivated types lifts it: so IsActive is explicit
                HasModels = x.EquipmentModels.Any(m => m.IsActive) };

        protected override void ConfigureSorting(SortBuilder<EquipmentTypeEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code).By("equipmentcategoryid", x => x.EquipmentCategoryId);

        protected override IQueryable<EquipmentTypeEntity> ApplyFilters(IQueryable<EquipmentTypeEntity> query, EquipmentTypeListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }

            if (request.EquipmentCategoryId.HasValue)
            {
                var equipmentCategoryId = request.EquipmentCategoryId.Value;
                query = query.Where(x => x.EquipmentCategoryId == equipmentCategoryId);
            }
            return query;
        }

        protected override async Task BeforeCreateAsync(EquipmentTypeCreateDto request, CancellationToken cancellationToken)
        {
            await RequireParentAsync(_categories, request.EquipmentCategoryId, x => x.EquipmentCategoryId, "دسته‌بندی تجهیزات", mustBeActive: true, cancellationToken);
        }

        protected override async Task BeforeUpdateAsync(EquipmentTypeEntity entity, EquipmentTypeUpdateDto request, CancellationToken cancellationToken)
        {
            // A parent must be active when it is being changed or when this record is being re-activated.
            var reactivating = !entity.IsActive && request.IsActive;
            var equipmentCategoryId = request.EquipmentCategoryId ?? entity.EquipmentCategoryId;
            await RequireParentAsync(_categories, equipmentCategoryId, x => x.EquipmentCategoryId, "دسته‌بندی تجهیزات", mustBeActive: equipmentCategoryId != entity.EquipmentCategoryId || reactivating, cancellationToken);
        }

        protected override EquipmentTypeEntity NewEntity(EquipmentTypeCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code), EquipmentCategoryId = request.EquipmentCategoryId };

        protected override void ApplyUpdate(EquipmentTypeEntity entity, EquipmentTypeUpdateDto request)
        {
            entity.EquipmentCategoryId = request.EquipmentCategoryId ?? entity.EquipmentCategoryId;
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override EquipmentTypeDetailDto ToDetail(EquipmentTypeEntity entity) => Map(entity, null);

        // The category title needs a query (the entity only carries the foreign key after a create / update); a deactivated category still has its title.
        protected override async Task<EquipmentTypeDetailDto> ToDetailAsync(EquipmentTypeEntity entity, CancellationToken cancellationToken)
        {
            var category = await _categories.GetByIdAsync(entity.EquipmentCategoryId, asNoTracking: true, cancellationToken);
            return Map(entity, category?.Title);
        }

        private static EquipmentTypeDetailDto Map(EquipmentTypeEntity entity, string? categoryTitle) =>
            new()
            {
                Id = entity.EquipmentTypeId,
                EquipmentCategoryId = entity.EquipmentCategoryId,
                EquipmentCategoryTitle = categoryTitle,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
