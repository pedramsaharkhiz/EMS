using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Interfaces.EquipmentStatus;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using EquipmentStatusEntity = EMS.Domain.Entities.EquipmentStatus;

namespace EMS.Application.Services.EquipmentStatus
{
    /// <summary>Catalog "وضعیت تجهیز". All CRUD behaviour comes from <see cref="CatalogService{TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>.</summary>
    public sealed class EquipmentStatusService
        : CatalogService<EquipmentStatusEntity, EquipmentStatusCreateDto, EquipmentStatusUpdateDto, EquipmentStatusDetailDto, EquipmentStatusListItemDto, EquipmentStatusListRequestDto>, IEquipmentStatusService
    {
        public EquipmentStatusService(IEquipmentStatusRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        public const string EntityLabel = "وضعیت تجهیز";
        protected override string Label => EntityLabel;
        protected override string DependentsMessage => "این وضعیت توسط تجهیز فعال استفاده می‌شود و قابل غیرفعال‌سازی نیست.";
        protected override Expression<Func<EquipmentStatusEntity, long>> IdSelector => x => x.EquipmentStatusId;

        protected override Expression<Func<EquipmentStatusEntity, EquipmentStatusListItemDto>> ListItem =>
            x => new EquipmentStatusListItemDto { Id = x.EquipmentStatusId, Title = x.Title, Code = x.Code, IsActive = x.IsActive };

        protected override void ConfigureSorting(SortBuilder<EquipmentStatusEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code);

        protected override IQueryable<EquipmentStatusEntity> ApplyFilters(IQueryable<EquipmentStatusEntity> query, EquipmentStatusListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override EquipmentStatusEntity NewEntity(EquipmentStatusCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code) };

        protected override void ApplyUpdate(EquipmentStatusEntity entity, EquipmentStatusUpdateDto request)
        {
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override EquipmentStatusDetailDto ToDetail(EquipmentStatusEntity entity) =>
            new()
            {
                Id = entity.EquipmentStatusId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
