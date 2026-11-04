using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Interfaces.EquipmentStatus;
using EMS.Domain;
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

        // The system rows (see EquipmentStatusCatalog) are what the code relies on: they stay active, keep their code, and
        // no other status may take one of their codes (it could be mistaken for the system status).
        protected override Task BeforeCreateAsync(EquipmentStatusCreateDto request, CancellationToken cancellationToken)
        {
            if (EquipmentStatusCatalog.IsSystemCode(request.Code))
                throw new ConflictException(ReservedCodeMessage, ErrorCodes.ReservedCode);
            return Task.CompletedTask;
        }

        protected override Task BeforeUpdateAsync(EquipmentStatusEntity entity, EquipmentStatusUpdateDto request, CancellationToken cancellationToken)
        {
            var system = EquipmentStatusCatalog.IsSystemRow(entity.EquipmentStatusId, entity.Code);
            var newCode = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

            if (system && newCode is not null && !string.Equals(newCode, entity.Code, StringComparison.Ordinal))
                throw new ConflictException("کد وضعیت‌های سیستمی قابل تغییر نیست.", ErrorCodes.ProtectedRecord);

            if (!system && EquipmentStatusCatalog.IsSystemCode(newCode))
                throw new ConflictException(ReservedCodeMessage, ErrorCodes.ReservedCode);

            return Task.CompletedTask;
        }

        protected override Task EnsureCanDeactivateAsync(EquipmentStatusEntity entity, CancellationToken cancellationToken) =>
            EquipmentStatusCatalog.IsSystemRow(entity.EquipmentStatusId, entity.Code)
                ? throw new ConflictException("وضعیت‌های سیستمی قابل غیرفعال‌سازی یا حذف نیستند (فقط عنوان آن‌ها قابل ویرایش است).", ErrorCodes.ProtectedRecord)
                : Task.CompletedTask;

        private const string ReservedCodeMessage = "این کد برای وضعیت‌های سیستمی رزرو شده است و برای وضعیت دیگری قابل استفاده نیست.";

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
