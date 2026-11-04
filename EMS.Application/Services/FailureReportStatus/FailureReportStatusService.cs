using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.FailureReportStatus;
using EMS.Application.Interfaces.FailureReportStatus;
using EMS.Domain;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using FailureReportStatusEntity = EMS.Domain.Entities.FailureReportStatus;

namespace EMS.Application.Services.FailureReportStatus
{
    /// <summary>
    /// Catalog "وضعیت گزارش خرابی". Paging, sorting, unique title, soft delete/restore, dependents check, optimistic concurrency,
    /// audit and error codes all come from CatalogService. Declare below only what is specific to this catalog.
    /// To add a parent (e.g. a Category) see EquipmentTypeService: inject its repository and use RequireParentAsync
    /// from BeforeCreateAsync / BeforeUpdateAsync. To filter the list by a column, override ApplyFilters.
    /// </summary>
    public sealed class FailureReportStatusService
        : CatalogService<FailureReportStatusEntity, FailureReportStatusCreateDto, FailureReportStatusUpdateDto, FailureReportStatusDetailDto, FailureReportStatusListItemDto, FailureReportStatusListRequestDto>, IFailureReportStatusService
    {
        public FailureReportStatusService(IFailureReportStatusRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        /// <summary>Persian name of the catalog; also used by its validators.</summary>
        public const string EntityLabel = "وضعیت گزارش خرابی";

        protected override string Label => EntityLabel;

        protected override string DependentsMessage => "این وضعیت در گزارش خرابی فعال استفاده شده است و قابل غیرفعال‌سازی نیست.";

        // The system rows (see FailureReportStatusCatalog) are what the code relies on: they stay active, keep their code, and no other
        // row may take one of their codes (it could be mistaken for the system row). Other rows are plain labels.
        protected override Task BeforeCreateAsync(FailureReportStatusCreateDto request, CancellationToken cancellationToken)
        {
            if (FailureReportStatusCatalog.IsSystemCode(request.Code))
                throw new ConflictException(ReservedCodeMessage, ErrorCodes.ReservedCode);
            return Task.CompletedTask;
        }

        protected override Task BeforeUpdateAsync(FailureReportStatusEntity entity, FailureReportStatusUpdateDto request, CancellationToken cancellationToken)
        {
            var system = FailureReportStatusCatalog.IsSystemRow(entity.FailureReportStatusId, entity.Code);
            var newCode = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

            if (system && newCode is not null && !string.Equals(newCode, entity.Code, StringComparison.Ordinal))
                throw new ConflictException("کد وضعیت‌های سیستمی قابل تغییر نیست.", ErrorCodes.ProtectedRecord);

            if (!system && FailureReportStatusCatalog.IsSystemCode(newCode))
                throw new ConflictException(ReservedCodeMessage, ErrorCodes.ReservedCode);

            return Task.CompletedTask;
        }

        protected override Task EnsureCanDeactivateAsync(FailureReportStatusEntity entity, CancellationToken cancellationToken) =>
            FailureReportStatusCatalog.IsSystemRow(entity.FailureReportStatusId, entity.Code)
                ? throw new ConflictException("وضعیت‌های سیستمی گزارش خرابی (باز، در حال رسیدگی، بسته) قابل غیرفعال‌سازی یا حذف نیستند (فقط عنوان آن‌ها قابل ویرایش است).", ErrorCodes.ProtectedRecord)
                : Task.CompletedTask;

        private const string ReservedCodeMessage = "این کد برای وضعیت‌های سیستمی رزرو شده است و برای وضعیت دیگری قابل استفاده نیست.";

        protected override Expression<Func<FailureReportStatusEntity, long>> IdSelector => x => x.FailureReportStatusId;

        protected override Expression<Func<FailureReportStatusEntity, FailureReportStatusListItemDto>> ListItem =>
            x => new FailureReportStatusListItemDto
            {
                Id = x.FailureReportStatusId,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive
            };

        // sortBy values the list accepts (besides "createddatetime"; the default order is by id)
        protected override void ConfigureSorting(SortBuilder<FailureReportStatusEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code);

        protected override IQueryable<FailureReportStatusEntity> ApplyFilters(IQueryable<FailureReportStatusEntity> query, FailureReportStatusListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override FailureReportStatusEntity NewEntity(FailureReportStatusCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code) };

        protected override void ApplyUpdate(FailureReportStatusEntity entity, FailureReportStatusUpdateDto request)
        {
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override FailureReportStatusDetailDto ToDetail(FailureReportStatusEntity entity) =>
            new()
            {
                Id = entity.FailureReportStatusId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
