using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Post;
using EMS.Application.Interfaces.Post;
using EMS.Domain;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;
using PostEntity = EMS.Domain.Entities.Post;

namespace EMS.Application.Services.Post
{
    /// <summary>
    /// Catalog "سمت". Paging, sorting, unique title, soft delete/restore, dependents check, optimistic concurrency,
    /// audit and error codes all come from CatalogService. Declare below only what is specific to this catalog.
    /// To add a parent (e.g. a Category) see EquipmentTypeService: inject its repository and use RequireParentAsync
    /// from BeforeCreateAsync / BeforeUpdateAsync. To filter the list by a column, override ApplyFilters.
    /// </summary>
    public sealed class PostService
        : CatalogService<PostEntity, PostCreateDto, PostUpdateDto, PostDetailDto, PostListItemDto, PostListRequestDto>, IPostService
    {
        public PostService(IPostRepository repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
            : base(repository, unitOfWork, audit) { }

        /// <summary>Persian name of the catalog; also used by its validators.</summary>
        public const string EntityLabel = "سمت";

        protected override string Label => EntityLabel;

        protected override string DependentsMessage => "این سمت به کاربر فعال اختصاص داده شده است و قابل غیرفعال‌سازی نیست. ابتدا سمت آن کاربران را عوض کنید.";

        // The system posts (see PostCatalog) are what the code relies on: they stay active, keep their code, and no other
        // post may take one of their codes (it could be mistaken for the system post). Posts are plain labels otherwise.
        protected override Task BeforeCreateAsync(PostCreateDto request, CancellationToken cancellationToken)
        {
            if (PostCatalog.IsSystemCode(request.Code))
                throw new ConflictException(ReservedCodeMessage, ErrorCodes.ReservedCode);
            return Task.CompletedTask;
        }

        protected override Task BeforeUpdateAsync(PostEntity entity, PostUpdateDto request, CancellationToken cancellationToken)
        {
            var system = PostCatalog.IsSystemRow(entity.PostId, entity.Code);
            var newCode = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

            if (system && newCode is not null && !string.Equals(newCode, entity.Code, StringComparison.Ordinal))
                throw new ConflictException("کد سمت‌های سیستمی قابل تغییر نیست.", ErrorCodes.ProtectedRecord);

            if (!system && PostCatalog.IsSystemCode(newCode))
                throw new ConflictException(ReservedCodeMessage, ErrorCodes.ReservedCode);

            return Task.CompletedTask;
        }

        protected override Task EnsureCanDeactivateAsync(PostEntity entity, CancellationToken cancellationToken) =>
            PostCatalog.IsSystemRow(entity.PostId, entity.Code)
                ? throw new ConflictException("سمت‌های سیستمی (تکنسین، مسئول واحد، ثبت‌کننده) قابل غیرفعال‌سازی یا حذف نیستند (فقط عنوان آن‌ها قابل ویرایش است).", ErrorCodes.ProtectedRecord)
                : Task.CompletedTask;

        private const string ReservedCodeMessage = "این کد برای سمت‌های سیستمی رزرو شده است و برای سمت دیگری قابل استفاده نیست.";

        protected override Expression<Func<PostEntity, long>> IdSelector => x => x.PostId;

        protected override Expression<Func<PostEntity, PostListItemDto>> ListItem =>
            x => new PostListItemDto
            {
                Id = x.PostId,
                Title = x.Title,
                Code = x.Code,
                IsActive = x.IsActive
            };

        // sortBy values the list accepts (besides "createddatetime"; the default order is by id)
        protected override void ConfigureSorting(SortBuilder<PostEntity> sort) =>
            sort.By("title", x => x.Title).By("code", x => x.Code);

        protected override IQueryable<PostEntity> ApplyFilters(IQueryable<PostEntity> query, PostListRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var search = request.Code.Trim();
                query = query.Where(x => x.Code!.Contains(search));
            }
            return query;
        }

        protected override PostEntity NewEntity(PostCreateDto request, string title) =>
            new() { Title = title, Code = Normalize(request.Code) };

        protected override void ApplyUpdate(PostEntity entity, PostUpdateDto request)
        {
            entity.Code = Keep(request.Code, entity.Code);
        }

        protected override PostDetailDto ToDetail(PostEntity entity) =>
            new()
            {
                Id = entity.PostId,
                Title = entity.Title,
                Code = entity.Code,
                IsActive = entity.IsActive,
                RowVersion = RowVersionToken.From(entity.ModifiedDateTime),
            };
    }
}
