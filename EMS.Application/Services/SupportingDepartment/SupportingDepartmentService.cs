using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Interfaces.SupportingDepartment;
using EMS.Application.Validators.SupportingDepartment;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using SupportingDepartmentEntity = EMS.Domain.Entities.SupportingDepartment;

namespace EMS.Application.Services.SupportingDepartment
{
    /// <summary>
    /// Catalog "دپارتمان پشتیبانی". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.
    /// A department title only has to be unique within its city.
    /// </summary>
    public sealed class SupportingDepartmentService
        : CatalogService<SupportingDepartmentEntity, SupportingDepartmentCreateDto, SupportingDepartmentUpdateDto, SupportingDepartmentDetailDto, SupportingDepartmentListItemDto, SupportingDepartmentListRequestDto>, ISupportingDepartmentService
    {
        private readonly ICityRepository _cities;
        private readonly ISupportingDepartmentRepository _departments;
        private readonly ISupportingDepartmentScopeService _scope;

        public SupportingDepartmentService(ISupportingDepartmentRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit, ICityRepository cities, ISupportingDepartmentScopeService scope)
            : base(repository, unitOfWork, audit)
        {
            _cities = cities;
            _departments = repository;
            _scope = scope;
        }

        public const string EntityLabel = "دپارتمان پشتیبانی";
        protected override string Label => EntityLabel;
        protected override string DuplicateMessage => "در این شهر دپارتمان پشتیبانی دیگری با این عنوان وجود دارد.";
        protected override string DependentsMessage => "این دپارتمان پشتیبانی دارای کاربر، تجهیز یا شعبه‌ی فعال است و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<SupportingDepartmentEntity, long>> IdSelector => x => x.SupportingDepartmentId;

        protected override Expression<Func<SupportingDepartmentEntity, SupportingDepartmentListItemDto>> ListItem =>
            x => new SupportingDepartmentListItemDto
            {
                Id = x.SupportingDepartmentId,
                CityId = x.CityId,
                CityTitle = x.City.Title,
                ProvinceId = x.City.ProvinceId,
                ProvinceTitle = x.City.Province.Title,
                Title = x.Title,
                IsActive = x.IsActive,
                OrganizationCount = x.Organizations.Count(o => o.IsActive)
            };

        protected override void ConfigureSorting(SortBuilder<SupportingDepartmentEntity> sort) =>
            sort.By("title", x => x.Title).By("cityid", x => x.CityId).By("citytitle", x => x.City.Title);

        protected override IQueryable<SupportingDepartmentEntity> ApplyFilters(IQueryable<SupportingDepartmentEntity> query, SupportingDepartmentListRequestDto request)
        {
            if (request.CityId.HasValue)
            {
                var cityId = request.CityId.Value;
                query = query.Where(x => x.CityId == cityId);
            }

            if (request.ProvinceId.HasValue)
            {
                var provinceId = request.ProvinceId.Value;
                query = query.Where(x => x.City.ProvinceId == provinceId);
            }
            return query;
        }

        protected override Expression<Func<SupportingDepartmentEntity, bool>>? TitleScopeForCreate(SupportingDepartmentCreateDto request) =>
            x => x.CityId == request.CityId;

        protected override Expression<Func<SupportingDepartmentEntity, bool>>? TitleScopeForUpdate(SupportingDepartmentEntity entity, SupportingDepartmentUpdateDto request)
        {
            var cityId = request.CityId ?? entity.CityId;
            return x => x.CityId == cityId;
        }

        protected override async Task BeforeCreateAsync(SupportingDepartmentCreateDto request, CancellationToken cancellationToken)
        {
            await RequireParentAsync(_cities, request.CityId, x => x.CityId, "شهر", mustBeActive: true, cancellationToken);
        }

        protected override async Task BeforeUpdateAsync(SupportingDepartmentEntity entity, SupportingDepartmentUpdateDto request, CancellationToken cancellationToken)
        {
            // A parent must be active when it is being changed or when this record is being re-activated.
            var reactivating = !entity.IsActive && request.IsActive;
            var cityId = request.CityId ?? entity.CityId;
            await RequireParentAsync(_cities, cityId, x => x.CityId, "شهر", mustBeActive: cityId != entity.CityId || reactivating, cancellationToken);
        }

        // Branches are optional. When given they are saved together with the department (one save = one transaction).
        protected override async Task OnCreatingAsync(SupportingDepartmentCreateDto request, SupportingDepartmentEntity entity, CancellationToken cancellationToken)
        {
            if (!SupportingDepartmentOrganizationsDtoValidator.HasSelection(request.Organizations))
                return;

            var organizations = await _scope.PrepareAssignmentAsync(null, request.Organizations!, "organizations.", cancellationToken);
            foreach (var organization in organizations)
                entity.Organizations.Add(organization);
        }

        // On update the branches are ADDED to the current ones; nothing is taken off (see the DELETE endpoint).
        protected override async Task OnUpdatingAsync(SupportingDepartmentEntity entity, SupportingDepartmentUpdateDto request, CancellationToken cancellationToken)
        {
            if (!SupportingDepartmentOrganizationsDtoValidator.HasSelection(request.Organizations))
                return;

            var organizations = await _scope.PrepareAssignmentAsync(entity.SupportingDepartmentId, request.Organizations!, "organizations.", cancellationToken);
            foreach (var organization in organizations.Where(o => o.SupportingDepartmentId != entity.SupportingDepartmentId))
                organization.SupportingDepartmentId = entity.SupportingDepartmentId;
        }

        protected override SupportingDepartmentEntity NewEntity(SupportingDepartmentCreateDto request, string title) =>
            new() { Title = title, CityId = request.CityId };

        protected override void ApplyUpdate(SupportingDepartmentEntity entity, SupportingDepartmentUpdateDto request)
        {
            entity.CityId = request.CityId ?? entity.CityId;
        }

        protected override SupportingDepartmentDetailDto ToDetail(SupportingDepartmentEntity entity) => Map(entity, null, 0);

        // The city and province titles need a query (the entity only carries the foreign key after a create / update).
        protected override async Task<SupportingDepartmentDetailDto> ToDetailAsync(SupportingDepartmentEntity entity, CancellationToken cancellationToken) =>
            Map(entity, await _cities.GetLocationAsync(entity.CityId, cancellationToken), await _departments.CountOrganizationsAsync(entity.SupportingDepartmentId, cancellationToken));

        private static SupportingDepartmentDetailDto Map(SupportingDepartmentEntity entity, CityLocation? location, int organizationCount) =>
            new()
            {
                Id = entity.SupportingDepartmentId,
                CityId = entity.CityId,
                CityTitle = location?.CityTitle ?? string.Empty,
                ProvinceId = location?.ProvinceId ?? 0,
                ProvinceTitle = location?.ProvinceTitle ?? string.Empty,
                Title = entity.Title,
                IsActive = entity.IsActive,
                OrganizationCount = organizationCount,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
