using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Interfaces.EquipmentLocationType;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using EquipmentLocationTypeEntity = EMS.Domain.Entities.EquipmentLocationType;

namespace EMS.Application.Services.EquipmentLocationType
{
    /// <summary>Catalog "نوع محل تجهیز". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class EquipmentLocationTypeService
        : CatalogService<EquipmentLocationTypeEntity, EquipmentLocationTypeCreateDto, EquipmentLocationTypeUpdateDto, EquipmentLocationTypeDetailDto, EquipmentLocationTypeListItemDto, EquipmentLocationTypeListRequestDto>, IEquipmentLocationTypeService
    {
        public EquipmentLocationTypeService(IEquipmentLocationTypeRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        protected override string Label => "نوع محل تجهیز";
        protected override string DependentsMessage => "این نوع مکان توسط تجهیز فعال استفاده می‌شود و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<EquipmentLocationTypeEntity, long>> IdSelector => x => x.EquipmentLocationTypeId;

        protected override Expression<Func<EquipmentLocationTypeEntity, EquipmentLocationTypeListItemDto>> ListItem =>
            x => new EquipmentLocationTypeListItemDto { EquipmentLocationTypeId = x.EquipmentLocationTypeId, Title = x.Title, Code = x.Code!, IsActive = x.IsActive };

        protected override void ConfigureSorting(SortBuilder<EquipmentLocationTypeEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code);

        protected override IQueryable<EquipmentLocationTypeEntity> ApplyFilters(IQueryable<EquipmentLocationTypeEntity> query, EquipmentLocationTypeListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override EquipmentLocationTypeEntity NewEntity(EquipmentLocationTypeCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code) };

        protected override void ApplyUpdate(EquipmentLocationTypeEntity entity, EquipmentLocationTypeUpdateDto request)
        {
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override EquipmentLocationTypeDetailDto ToDetail(EquipmentLocationTypeEntity entity) =>
            new()
            {
                EquipmentLocationTypeId = entity.EquipmentLocationTypeId,
                Title = entity.Title,
                Code = entity.Code!,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
