using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Brand;
using EMS.Application.Interfaces.Brand;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using BrandEntity = EMS.Domain.Entities.Brand;

namespace EMS.Application.Services.Brand
{
    /// <summary>Catalog "برند". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class BrandService
        : CatalogService<BrandEntity, BrandCreateDto, BrandUpdateDto, BrandDetailDto, BrandListItemDto, BrandListRequestDto>, IBrandService
    {
        public BrandService(IBrandRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        public const string EntityLabel = "برند";
        protected override string Label => EntityLabel;
        protected override string DependentsMessage => "این برند دارای مدل تجهیز فعال است و قابل غیرفعال‌سازی نیست. ابتدا مدل‌های آن را غیرفعال کنید.";
        protected override Expression<Func<BrandEntity, long>> IdSelector => x => x.BrandId;

        protected override Expression<Func<BrandEntity, BrandListItemDto>> ListItem =>
            x => new BrandListItemDto { Id = x.BrandId, Title = x.Title, Code = x.Code, IsActive = x.IsActive };

        protected override void ConfigureSorting(SortBuilder<BrandEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code);

        protected override IQueryable<BrandEntity> ApplyFilters(IQueryable<BrandEntity> query, BrandListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override BrandEntity NewEntity(BrandCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code) };

        protected override void ApplyUpdate(BrandEntity entity, BrandUpdateDto request)
        {
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override BrandDetailDto ToDetail(BrandEntity entity) =>
            new()
            {
                Id = entity.BrandId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
