using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.City;
using EMS.Application.Interfaces.City;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using CityEntity = EMS.Domain.Entities.City;

namespace EMS.Application.Services.City
{
    /// <summary>
    /// Catalog "شهر". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.
    /// A city title only has to be unique within its province.
    /// </summary>
    public sealed class CityService
        : CatalogService<CityEntity, CityCreateDto, CityUpdateDto, CityDetailDto, CityListItemDto, CityListRequestDto>, ICityService
    {
        private readonly IProvinceRepository _provinces;

        public CityService(ICityRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit, IProvinceRepository provinces)
            : base(repository, unitOfWork, audit)
        {
            _provinces = provinces;
        }

        public const string EntityLabel = "شهر";
        protected override string Label => EntityLabel;
        protected override string DuplicateMessage => "در این استان شهر دیگری با این عنوان وجود دارد.";
        protected override string DependentsMessage => "این شهر دارای دپارتمان پشتیبانی فعال است و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<CityEntity, long>> IdSelector => x => x.CityId;

        protected override Expression<Func<CityEntity, CityListItemDto>> ListItem =>
            x => new CityListItemDto
            {
                Id = x.CityId,
                ProvinceId = x.ProvinceId,
                ProvinceTitle = x.Province.Title,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive
            };

        protected override void ConfigureSorting(SortBuilder<CityEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code).By("provinceid", x => x.ProvinceId).By("provincetitle", x => x.Province.Title);

        protected override IQueryable<CityEntity> ApplyFilters(IQueryable<CityEntity> query, CityListRequestDto request)
        {
            if (request.ProvinceId.HasValue)
            {
                var provinceId = request.ProvinceId.Value;
                query = query.Where(x => x.ProvinceId == provinceId);
            }

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override Expression<Func<CityEntity, bool>>? TitleScopeForCreate(CityCreateDto request) =>
            x => x.ProvinceId == request.ProvinceId;

        protected override Expression<Func<CityEntity, bool>>? TitleScopeForUpdate(CityEntity entity, CityUpdateDto request)
        {
            var provinceId = request.ProvinceId ?? entity.ProvinceId;
            return x => x.ProvinceId == provinceId;
        }

        protected override async Task BeforeCreateAsync(CityCreateDto request, CancellationToken cancellationToken)
        {
            await RequireParentAsync(_provinces, request.ProvinceId, x => x.ProvinceId, "استان", mustBeActive: true, cancellationToken);
        }

        protected override async Task BeforeUpdateAsync(CityEntity entity, CityUpdateDto request, CancellationToken cancellationToken)
        {
            // A parent must be active when it is being changed or when this record is being re-activated.
            var reactivating = !entity.IsActive && request.IsActive;
            var provinceId = request.ProvinceId ?? entity.ProvinceId;
            await RequireParentAsync(_provinces, provinceId, x => x.ProvinceId, "استان", mustBeActive: provinceId != entity.ProvinceId || reactivating, cancellationToken);
        }

        protected override CityEntity NewEntity(CityCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code), ProvinceId = request.ProvinceId };

        protected override void ApplyUpdate(CityEntity entity, CityUpdateDto request)
        {
            entity.ProvinceId = request.ProvinceId ?? entity.ProvinceId;
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override CityDetailDto ToDetail(CityEntity entity) => Map(entity, string.Empty);

        // The province title needs a query (the entity only carries the foreign key after a create / update).
        protected override async Task<CityDetailDto> ToDetailAsync(CityEntity entity, CancellationToken cancellationToken)
        {
            var province = await _provinces.GetByIdAsync(entity.ProvinceId, asNoTracking: true, cancellationToken);
            return Map(entity, province?.Title ?? string.Empty);
        }

        private static CityDetailDto Map(CityEntity entity, string provinceTitle) =>
            new()
            {
                Id = entity.CityId,
                ProvinceId = entity.ProvinceId,
                ProvinceTitle = provinceTitle,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
