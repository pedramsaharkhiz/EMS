using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Interfaces.EquipmentType;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using EquipmentTypeEntity = EMS.Domain.Entities.EquipmentType;

namespace EMS.Application.Services.EquipmentType
{
    /// <summary>Catalog "نوع تجهیز". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class EquipmentTypeService
        : CatalogService<EquipmentTypeEntity, EquipmentTypeCreateDto, EquipmentTypeUpdateDto, EquipmentTypeDetailDto, EquipmentTypeListItemDto, EquipmentTypeListRequestDto>, IEquipmentTypeService
    {
        private readonly IEquipmentCategoryRepository _categories;
        public EquipmentTypeService(IEquipmentTypeRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit, IEquipmentCategoryRepository categories)
            : base(repository, unitOfWork, audit)
        {
            _categories = categories;
        }

        public const string EntityLabel = "نوع تجهیز";
        protected override string Label => EntityLabel;
        protected override string DependentsMessage => "این نوع تجهیز دارای مدل یا تجهیز فعال است و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<EquipmentTypeEntity, long>> IdSelector => x => x.EquipmentTypeId;

        protected override Expression<Func<EquipmentTypeEntity, EquipmentTypeListItemDto>> ListItem =>
            x => new EquipmentTypeListItemDto { EquipmentTypeId = x.EquipmentTypeId, EquipmentCategoryId = x.EquipmentCategoryId, Title = x.Title, Code = x.Code!, IsActive = x.IsActive };

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

        protected override EquipmentTypeDetailDto ToDetail(EquipmentTypeEntity entity) =>
            new()
            {
                EquipmentTypeId = entity.EquipmentTypeId,
                EquipmentCategoryId = entity.EquipmentCategoryId,
                Title = entity.Title,
                Code = entity.Code!,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
