using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Province;
using EMS.Application.Interfaces.Province;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using ProvinceEntity = EMS.Domain.Entities.Province;

namespace EMS.Application.Services.Province
{
    /// <summary>Catalog "استان". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class ProvinceService
        : CatalogService<ProvinceEntity, ProvinceCreateDto, ProvinceUpdateDto, ProvinceDetailDto, ProvinceListItemDto, ProvinceListRequestDto>, IProvinceService
    {
        public ProvinceService(IProvinceRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        /// <summary>Persian name of the catalog; also used by its validators.</summary>
        public const string EntityLabel = "استان";

        protected override string Label => EntityLabel;

        protected override string DependentsMessage => "این استان دارای شهر فعال است و قابل غیرفعال‌سازی نیست.";

        protected override Expression<Func<ProvinceEntity, long>> IdSelector => x => x.ProvinceId;

        protected override Expression<Func<ProvinceEntity, ProvinceListItemDto>> ListItem =>
            x => new ProvinceListItemDto
            {
                Id = x.ProvinceId,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive
            };

        // sortBy values the list accepts (besides "createddatetime"; the default order is by id)
        protected override void ConfigureSorting(SortBuilder<ProvinceEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code);

        protected override IQueryable<ProvinceEntity> ApplyFilters(IQueryable<ProvinceEntity> query, ProvinceListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override ProvinceEntity NewEntity(ProvinceCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code) };

        protected override void ApplyUpdate(ProvinceEntity entity, ProvinceUpdateDto request)
        {
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override ProvinceDetailDto ToDetail(ProvinceEntity entity) =>
            new()
            {
                Id = entity.ProvinceId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
