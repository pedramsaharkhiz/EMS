using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.FailureCategory;
using EMS.Application.Interfaces.FailureCategory;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using FailureCategoryEntity = EMS.Domain.Entities.FailureCategory;

namespace EMS.Application.Services.FailureCategory
{
    /// <summary>Catalog "دسته‌بندی خرابی". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class FailureCategoryService
        : CatalogService<FailureCategoryEntity, FailureCategoryCreateDto, FailureCategoryUpdateDto, FailureCategoryDetailDto, FailureCategoryListItemDto, FailureCategoryListRequestDto>, IFailureCategoryService
    {
        public FailureCategoryService(IFailureCategoryRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        public const string EntityLabel = "دسته‌بندی خرابی";
        protected override string Label => EntityLabel;
        protected override string DependentsMessage => "این دسته‌بندی دارای نوع خرابی فعال است و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<FailureCategoryEntity, long>> IdSelector => x => x.FailureCategoryId;

        protected override Expression<Func<FailureCategoryEntity, FailureCategoryListItemDto>> ListItem =>
            x => new FailureCategoryListItemDto { Id = x.FailureCategoryId, Title = x.Title, IsActive = x.IsActive };

        protected override void ConfigureSorting(SortBuilder<FailureCategoryEntity> sort) =>
            sort.By("title", x => x.Title);

        protected override FailureCategoryEntity NewEntity(FailureCategoryCreateDto request, string title) =>
            new() { Title = title };

        protected override void ApplyUpdate(FailureCategoryEntity entity, FailureCategoryUpdateDto request)
        {
            // Title and IsActive are handled by CatalogService; this catalog has no other fields.
        }

        protected override FailureCategoryDetailDto ToDetail(FailureCategoryEntity entity) =>
            new()
            {
                Id = entity.FailureCategoryId,
                Title = entity.Title,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
