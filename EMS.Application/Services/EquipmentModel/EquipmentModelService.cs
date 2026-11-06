using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Interfaces.EquipmentModel;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using EquipmentModelEntity = EMS.Domain.Entities.EquipmentModel;

namespace EMS.Application.Services.EquipmentModel
{
    /// <summary>Catalog "مدل تجهیز". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class EquipmentModelService
        : CatalogService<EquipmentModelEntity, EquipmentModelCreateDto, EquipmentModelUpdateDto, EquipmentModelDetailDto, EquipmentModelListItemDto, EquipmentModelListRequestDto>, IEquipmentModelService
    {
        private readonly IEquipmentTypeRepository _types;
        private readonly IBrandRepository _brands;
        public EquipmentModelService(IEquipmentModelRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit, IEquipmentTypeRepository types, IBrandRepository brands)
            : base(repository, unitOfWork, audit)
        {
            _types = types;
            _brands = brands;
        }

        public const string EntityLabel = "مدل تجهیز";
        protected override string Label => EntityLabel;
        protected override string DependentsMessage => "این مدل توسط تجهیز فعال استفاده می‌شود و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<EquipmentModelEntity, long>> IdSelector => x => x.EquipmentModelId;

        protected override Expression<Func<EquipmentModelEntity, EquipmentModelListItemDto>> ListItem =>
            x => new EquipmentModelListItemDto { Id = x.EquipmentModelId, EquipmentTypeId = x.EquipmentTypeId, EquipmentTypeTitle = x.EquipmentType.Title, BrandId = x.BrandId, BrandTitle = x.Brand.Title, Title = x.Title, Code = x.Code, IsActive = x.IsActive };

        protected override void ConfigureSorting(SortBuilder<EquipmentModelEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code).By("equipmenttypeid", x => x.EquipmentTypeId).By("brandid", x => x.BrandId);

        protected override IQueryable<EquipmentModelEntity> ApplyFilters(IQueryable<EquipmentModelEntity> query, EquipmentModelListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }

            if (request.EquipmentTypeId.HasValue)
            {
                var equipmentTypeId = request.EquipmentTypeId.Value;
                query = query.Where(x => x.EquipmentTypeId == equipmentTypeId);
            }

            if (request.BrandId.HasValue)
            {
                var brandId = request.BrandId.Value;
                query = query.Where(x => x.BrandId == brandId);
            }
            return query;
        }

        protected override async Task BeforeCreateAsync(EquipmentModelCreateDto request, CancellationToken cancellationToken)
        {
            await RequireParentAsync(_types, request.EquipmentTypeId, x => x.EquipmentTypeId, "نوع تجهیزات", mustBeActive: true, cancellationToken);
            await RequireParentAsync(_brands, request.BrandId, x => x.BrandId, "برند", mustBeActive: true, cancellationToken);
        }

        protected override async Task BeforeUpdateAsync(EquipmentModelEntity entity, EquipmentModelUpdateDto request, CancellationToken cancellationToken)
        {
            // A parent must be active when it is being changed or when this record is being re-activated.
            var reactivating = !entity.IsActive && request.IsActive;
            var equipmentTypeId = request.EquipmentTypeId ?? entity.EquipmentTypeId;
            await RequireParentAsync(_types, equipmentTypeId, x => x.EquipmentTypeId, "نوع تجهیزات", mustBeActive: equipmentTypeId != entity.EquipmentTypeId || reactivating, cancellationToken);
            var brandId = request.BrandId ?? entity.BrandId;
            await RequireParentAsync(_brands, brandId, x => x.BrandId, "برند", mustBeActive: brandId != entity.BrandId || reactivating, cancellationToken);
        }

        protected override EquipmentModelEntity NewEntity(EquipmentModelCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code), EquipmentTypeId = request.EquipmentTypeId, BrandId = request.BrandId };

        protected override void ApplyUpdate(EquipmentModelEntity entity, EquipmentModelUpdateDto request)
        {
            entity.EquipmentTypeId = request.EquipmentTypeId ?? entity.EquipmentTypeId;
            entity.BrandId = request.BrandId ?? entity.BrandId;
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override EquipmentModelDetailDto ToDetail(EquipmentModelEntity entity) => Map(entity, null, null);

        // The parents' titles need queries (the entity only carries the foreign keys after a create / update); a deactivated parent still has its title.
        protected override async Task<EquipmentModelDetailDto> ToDetailAsync(EquipmentModelEntity entity, CancellationToken cancellationToken)
        {
            var type = await _types.GetByIdAsync(entity.EquipmentTypeId, asNoTracking: true, cancellationToken);
            var brand = await _brands.GetByIdAsync(entity.BrandId, asNoTracking: true, cancellationToken);
            return Map(entity, type?.Title, brand?.Title);
        }

        private static EquipmentModelDetailDto Map(EquipmentModelEntity entity, string? typeTitle, string? brandTitle) =>
            new()
            {
                Id = entity.EquipmentModelId,
                EquipmentTypeId = entity.EquipmentTypeId,
                EquipmentTypeTitle = typeTitle,
                BrandId = entity.BrandId,
                BrandTitle = brandTitle,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
