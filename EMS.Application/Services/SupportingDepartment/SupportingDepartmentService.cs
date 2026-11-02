using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Interfaces.SupportingDepartment;
using EMS.Domain.Interfaces;
using EMS.Domain.Entities;
using System.Linq.Expressions;
using SupportingDepartmentEntity = EMS.Domain.Entities.SupportingDepartment;

namespace EMS.Application.Services.SupportingDepartment
{
    /// <summary>Catalog "دپارتمان پشتیبانی". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class SupportingDepartmentService
        : CatalogService<SupportingDepartmentEntity, SupportingDepartmentCreateDto, SupportingDepartmentUpdateDto, SupportingDepartmentDetailDto, SupportingDepartmentListItemDto, SupportingDepartmentListRequestDto>, ISupportingDepartmentService
    {
        private readonly IRepository<City> _cities;
        public SupportingDepartmentService(ISupportingDepartmentRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit, IRepository<City> cities)
            : base(repository, unitOfWork, audit)
        {
            _cities = cities;
        }

        protected override string Label => "دپارتمان پشتیبانی";
        protected override string DependentsMessage => "این دپارتمان پشتیبانی دارای کاربر یا تجهیز فعال است و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<SupportingDepartmentEntity, long>> IdSelector => x => x.SupportingDepartmentId;

        protected override Expression<Func<SupportingDepartmentEntity, SupportingDepartmentListItemDto>> ListItem =>
            x => new SupportingDepartmentListItemDto { SupportingDepartmentId = x.SupportingDepartmentId, CityId = x.CityId, Title = x.Title, IsActive = x.IsActive };

        protected override void ConfigureSorting(SortBuilder<SupportingDepartmentEntity> sort) =>
            sort.By("title", x => x.Title).By("cityid", x => x.CityId);

        protected override IQueryable<SupportingDepartmentEntity> ApplyFilters(IQueryable<SupportingDepartmentEntity> query, SupportingDepartmentListRequestDto request)
        {
            if (request.CityId.HasValue)
            {
                var cityId = request.CityId.Value;
                query = query.Where(x => x.CityId == cityId);
            }
            return query;
        }

        protected override async Task BeforeCreateAsync(SupportingDepartmentCreateDto request, CancellationToken cancellationToken)
        {
            await RequireParentAsync(_cities, request.CityId, x => x.CityId, "شهر", mustBeActive: false, cancellationToken);
        }

        protected override async Task BeforeUpdateAsync(SupportingDepartmentEntity entity, SupportingDepartmentUpdateDto request, CancellationToken cancellationToken)
        {
            // A parent must be active when it is being changed or when this record is being re-activated.
            var reactivating = !entity.IsActive && request.IsActive;
            await RequireParentAsync(_cities, request.CityId ?? entity.CityId, x => x.CityId, "شهر", mustBeActive: false, cancellationToken);
        }

        protected override SupportingDepartmentEntity NewEntity(SupportingDepartmentCreateDto request, string title) =>
            new() { Title = title, CityId = request.CityId };

        protected override void ApplyUpdate(SupportingDepartmentEntity entity, SupportingDepartmentUpdateDto request)
        {
            entity.CityId = request.CityId ?? entity.CityId;
        }

        protected override SupportingDepartmentDetailDto ToDetail(SupportingDepartmentEntity entity) =>
            new()
            {
                SupportingDepartmentId = entity.SupportingDepartmentId,
                CityId = entity.CityId,
                Title = entity.Title,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
