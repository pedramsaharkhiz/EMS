using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using EquipmentCategoryEntity = EMS.Domain.Entities.EquipmentCategory;

namespace EMS.Application.Services.EquipmentCategory
{
    /// <summary>Catalog "دسته‌بندی". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class EquipmentCategoryService
        : CatalogService<EquipmentCategoryEntity, EquipmentCategoryCreateDto, EquipmentCategoryUpdateDto, EquipmentCategoryDetailDto, EquipmentCategoryListItemDto, EquipmentCategoryListRequestDto>, IEquipmentCategoryService
    {
        public EquipmentCategoryService(IEquipmentCategoryRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        public const string EntityLabel = "دسته‌بندی";
        protected override string Label => EntityLabel;
        protected override string DependentsMessage => "این دسته‌بندی دارای نوع تجهیز فعال است و قابل غیرفعال‌سازی نیست. ابتدا نوع‌های آن را غیرفعال کنید.";
        protected override Expression<Func<EquipmentCategoryEntity, long>> IdSelector => x => x.EquipmentCategoryId;

        protected override Expression<Func<EquipmentCategoryEntity, EquipmentCategoryListItemDto>> ListItem =>
            x => new EquipmentCategoryListItemDto { Id = x.EquipmentCategoryId, Title = x.Title, IsActive = x.IsActive };

        protected override void ConfigureSorting(SortBuilder<EquipmentCategoryEntity> sort) =>
            sort.By("title", x => x.Title);

        protected override EquipmentCategoryEntity NewEntity(EquipmentCategoryCreateDto request, string title) =>
            new() { Title = title };

        protected override void ApplyUpdate(EquipmentCategoryEntity entity, EquipmentCategoryUpdateDto request)
        {
            // Title and IsActive are handled by CatalogService; this catalog has no other fields.
        }

        protected override EquipmentCategoryDetailDto ToDetail(EquipmentCategoryEntity entity) =>
            new()
            {
                Id = entity.EquipmentCategoryId,
                Title = entity.Title,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
