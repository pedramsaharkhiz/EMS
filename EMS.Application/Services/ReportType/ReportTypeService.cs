using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.ReportType;
using EMS.Application.Interfaces.ReportType;
using EMS.Domain;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using ReportTypeEntity = EMS.Domain.Entities.ReportType;

namespace EMS.Application.Services.ReportType
{
    /// <summary>
    /// Catalog "نوع گزارش". Paging, sorting, unique title, soft delete/restore, dependents check, optimistic concurrency,
    /// audit and error codes all come from CatalogService. Declare below only what is specific to this catalog.
    /// To add a parent (e.g. a Category) see EquipmentTypeService: inject its repository and use RequireParentAsync
    /// from BeforeCreateAsync / BeforeUpdateAsync. To filter the list by a column, override ApplyFilters.
    /// </summary>
    public sealed class ReportTypeService
        : CatalogService<ReportTypeEntity, ReportTypeCreateDto, ReportTypeUpdateDto, ReportTypeDetailDto, ReportTypeListItemDto, ReportTypeListRequestDto>, IReportTypeService
    {
        public ReportTypeService(IReportTypeRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        /// <summary>Persian name of the catalog; also used by its validators.</summary>
        public const string EntityLabel = "نوع گزارش";

        protected override string Label => EntityLabel;

        protected override string DependentsMessage => "این نوع گزارش در گزارش خرابی فعال استفاده شده است و قابل غیرفعال‌سازی نیست.";

        // The system rows (see ReportTypeCatalog) are what the code relies on: they stay active, keep their code, and no other
        // row may take one of their codes (it could be mistaken for the system row). Other rows are plain labels.
        protected override Task BeforeCreateAsync(ReportTypeCreateDto request, CancellationToken cancellationToken)
        {
            if (ReportTypeCatalog.IsSystemCode(request.Code))
                throw new ConflictException(ReservedCodeMessage, ErrorCodes.ReservedCode);
            return Task.CompletedTask;
        }

        protected override Task BeforeUpdateAsync(ReportTypeEntity entity, ReportTypeUpdateDto request, CancellationToken cancellationToken)
        {
            var system = ReportTypeCatalog.IsSystemRow(entity.ReportTypeId, entity.Code);
            var newCode = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

            if (system && newCode is not null && !string.Equals(newCode, entity.Code, StringComparison.Ordinal))
                throw new ConflictException("کد نوع گزارش‌های سیستمی قابل تغییر نیست.", ErrorCodes.ProtectedRecord);

            if (!system && ReportTypeCatalog.IsSystemCode(newCode))
                throw new ConflictException(ReservedCodeMessage, ErrorCodes.ReservedCode);

            return Task.CompletedTask;
        }

        protected override Task EnsureCanDeactivateAsync(ReportTypeEntity entity, CancellationToken cancellationToken) =>
            ReportTypeCatalog.IsSystemRow(entity.ReportTypeId, entity.Code)
                ? throw new ConflictException("نوع گزارش‌های سیستمی قابل غیرفعال‌سازی یا حذف نیستند (فقط عنوان آن‌ها قابل ویرایش است).", ErrorCodes.ProtectedRecord)
                : Task.CompletedTask;

        private const string ReservedCodeMessage = "این کد برای نوع گزارش‌های سیستمی رزرو شده است و برای نوع دیگری قابل استفاده نیست.";

        protected override Expression<Func<ReportTypeEntity, long>> IdSelector => x => x.ReportTypeId;

        protected override Expression<Func<ReportTypeEntity, ReportTypeListItemDto>> ListItem =>
            x => new ReportTypeListItemDto
            {
                Id = x.ReportTypeId,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive
            };

        // sortBy values the list accepts (besides "createddatetime"; the default order is by id)
        protected override void ConfigureSorting(SortBuilder<ReportTypeEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code);

        protected override IQueryable<ReportTypeEntity> ApplyFilters(IQueryable<ReportTypeEntity> query, ReportTypeListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override ReportTypeEntity NewEntity(ReportTypeCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code) };

        protected override void ApplyUpdate(ReportTypeEntity entity, ReportTypeUpdateDto request)
        {
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override ReportTypeDetailDto ToDetail(ReportTypeEntity entity) =>
            new()
            {
                Id = entity.ReportTypeId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
