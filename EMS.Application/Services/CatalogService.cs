using EMS.Application.Common.Catalog;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Extensions;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using System.Linq.Expressions;

namespace EMS.Application.Services
{
    /// <summary>
    /// The complete CRUD behaviour of a catalog (lookup / master-data) entity, written once:
    /// paging, sorting, title filter, unique title among active records, soft delete and restore, deactivation guarded by
    /// dependents, optimistic concurrency, audit logging and consistent error codes.
    /// A concrete catalog only declares what is specific to it (its label, columns, parents, dependents) - see
    /// <c>BrandService</c> for the smallest example and docs/CATALOGS.md for how to add a new one.
    /// </summary>
    public abstract class CatalogService<TEntity, TCreate, TUpdate, TDetail, TListItem, TListRequest>
        : ICatalogService<TCreate, TUpdate, TDetail, TListItem, TListRequest>
        where TEntity : BaseEntity, ICatalogEntity
        where TCreate : ICatalogCreateRequest
        where TUpdate : ICatalogUpdateRequest
        where TListRequest : PagedRequest, ICatalogListRequest
    {
        private readonly ICatalogRepository<TEntity> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;
        private readonly Lazy<Func<TEntity, long>> _idOf;
        private readonly Lazy<SortBuilder<TEntity>> _sorting;

        protected CatalogService(ICatalogRepository<TEntity> repository, IUnitOfWork unitOfWork, IAuditEventLogger audit)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _audit = audit;
            _idOf = new Lazy<Func<TEntity, long>>(() => IdSelector.Compile());
            _sorting = new Lazy<SortBuilder<TEntity>>(() =>
            {
                var sort = new SortBuilder<TEntity>();
                sort.By("createddatetime", x => x.CreatedDateTime);
                ConfigureSorting(sort);
                return sort;
            });
        }

        // ------------------------------------------------------------------------------------------------
        // What a concrete catalog declares
        // ------------------------------------------------------------------------------------------------

        /// <summary>Persian name of the entity, used in messages ("برند").</summary>
        protected abstract string Label { get; }

        /// <summary>Message shown when the record cannot be deactivated because active records depend on it.</summary>
        protected abstract string DependentsMessage { get; }

        /// <summary>The primary key, e.g. <c>x =&gt; x.BrandId</c>. Also the default sort order.</summary>
        protected abstract Expression<Func<TEntity, long>> IdSelector { get; }

        /// <summary>Projection used by the list (translated to SQL).</summary>
        protected abstract Expression<Func<TEntity, TListItem>> ListItem { get; }

        protected abstract TDetail ToDetail(TEntity entity);

        /// <summary>Creates the entity. <paramref name="title"/> is already trimmed; set the other fields here.</summary>
        protected abstract TEntity NewEntity(TCreate request, string title);

        /// <summary>Applies the fields other than Title and IsActive (those are handled here).</summary>
        protected abstract void ApplyUpdate(TEntity entity, TUpdate request);

        /// <summary>Extra list filters besides Title (e.g. by parent id). Optional.</summary>
        protected virtual IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query, TListRequest request) => query;

        /// <summary>Columns the list can be sorted by besides "createddatetime" (default order is by id). Optional.</summary>
        protected virtual void ConfigureSorting(SortBuilder<TEntity> sort) { }

        /// <summary>Validate parents before creating (see <see cref="RequireParentAsync{TParent}"/>). Optional.</summary>
        protected virtual Task BeforeCreateAsync(TCreate request, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>Validate parents before updating. Optional.</summary>
        protected virtual Task BeforeUpdateAsync(TEntity entity, TUpdate request, CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual string DuplicateMessage => $"{Label} دیگری با این عنوان وجود دارد.";
        protected virtual string NotFoundMessage => $"{Label} موردنظر پیدا نشد.";

        // ------------------------------------------------------------------------------------------------
        // Behaviour shared by every catalog
        // ------------------------------------------------------------------------------------------------

        public async Task<PagedResult<TListItem>> GetPagedAsync(TListRequest request, CancellationToken cancellationToken = default)
        {
            // Active records by default; ?isActive=false lists the deactivated ones (so they can be found and restored).
            var query = _repository.QueryByActive(request.IsActive ?? true);

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                var search = request.Title.Trim();
                query = query.Where(x => x.Title.Contains(search));
            }

            query = ApplyFilters(query, request);
            query = ApplySorting(query, request.SortBy, request.SortDirection);

            return await query.Select(ListItem).ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public async Task<TDetail> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await LoadAsync(id, asNoTracking: true, cancellationToken);
            return ToDetail(entity);
        }

        public async Task<TDetail> CreateAsync(TCreate request, CancellationToken cancellationToken = default)
        {
            await BeforeCreateAsync(request, cancellationToken);

            var title = request.Title.Trim();
            await EnsureTitleIsFreeAsync(title, exceptId: null, cancellationToken);

            var entity = NewEntity(request, title);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Created(typeof(TEntity).Name, _idOf.Value(entity));
            return ToDetail(entity);
        }

        public async Task<TDetail> UpdateAsync(long id, TUpdate request, CancellationToken cancellationToken = default)
        {
            // The id comes from the route, so the request validators cannot see it: reject a non-positive id here, as a
            // validation error on the "id" field (the same response a body-level validator would give).
            if (id <= 0)
                throw new ValidationException(new[] { new ValidationFailure("id", $"شناسه {Label} باید بزرگتر از صفر باشد.") });

            var entity = await LoadAsync(id, asNoTracking: false, cancellationToken);
            await BeforeUpdateAsync(entity, request, cancellationToken);

            var title = Keep(request.Title, entity.Title)!;

            // Only an active record reserves its title (consistent with the filtered unique index in the database).
            if (request.IsActive)
                await EnsureTitleIsFreeAsync(title, _idOf.Value(entity), cancellationToken);

            entity.Title = title;
            ApplyUpdate(entity, request);

            if (RowVersionToken.TryParse(request.RowVersion, out var expectedVersion))
                _repository.SetExpectedModifiedDateTime(entity, expectedVersion);

            if (entity.IsActive && !request.IsActive)
                await EnsureNoActiveDependentsAsync(entity, cancellationToken);

            entity.IsActive = request.IsActive;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Updated(typeof(TEntity).Name, _idOf.Value(entity));
            return ToDetail(entity);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var entity = await LoadAsync(id, asNoTracking: false, cancellationToken);

            if (entity.IsActive)
                await EnsureNoActiveDependentsAsync(entity, cancellationToken);

            entity.IsActive = false;
            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Deleted(typeof(TEntity).Name, _idOf.Value(entity));
        }

        // ------------------------------------------------------------------------------------------------
        // Helpers (also available to concrete catalogs)
        // ------------------------------------------------------------------------------------------------

        /// <summary>A text field in an update request: a blank or missing value keeps the current one, otherwise it is trimmed.</summary>
        protected static string? Keep(string? incoming, string? current) =>
            string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();

        /// <summary>A text field in a create request: trimmed, null stays null.</summary>
        protected static string? Normalize(string? value) => value?.Trim();

        /// <summary>
        /// Requires the parent record to exist (404) and, when <paramref name="mustBeActive"/>, to be active (400).
        /// Use it from <see cref="BeforeCreateAsync"/> / <see cref="BeforeUpdateAsync"/>.
        /// </summary>
        protected static async Task RequireParentAsync<TParent>(
            IRepository<TParent> repository,
            long id,
            Expression<Func<TParent, long>> key,
            string label,
            bool mustBeActive,
            CancellationToken cancellationToken)
            where TParent : BaseEntity
        {
            var parameter = key.Parameters[0];
            var matches = Expression.Equal(key.Body, Expression.Constant(id));

            var exists = Expression.Lambda<Func<TParent, bool>>(matches, parameter);
            if (!await repository.ExistsIncludingInactiveAsync(exists, cancellationToken))
                throw new NotFoundException($"{label} یافت نشد.", ErrorCodes.ParentNotFound);

            if (!mustBeActive)
                return;

            var existsAndActive = Expression.Lambda<Func<TParent, bool>>(
                Expression.AndAlso(matches, Expression.Property(parameter, nameof(BaseEntity.IsActive))), parameter);
            if (!await repository.ExistsAsync(existsAndActive, cancellationToken))
                throw new BadRequestException($"{label} انتخاب‌شده غیرفعال است.", ErrorCodes.ParentInactive);
        }

        private async Task<TEntity> LoadAsync(long id, bool asNoTracking, CancellationToken cancellationToken) =>
            await _repository.GetByIdAsync(id, asNoTracking, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage, ErrorCodes.NotFound);

        private async Task EnsureTitleIsFreeAsync(string title, long? exceptId, CancellationToken cancellationToken)
        {
            if (await _repository.ExistsByTitleAsync(title, exceptId, cancellationToken))
                throw new ConflictException(DuplicateMessage, ErrorCodes.DuplicateTitle);
        }

        private async Task EnsureNoActiveDependentsAsync(TEntity entity, CancellationToken cancellationToken)
        {
            if (await _repository.HasActiveDependentsAsync(_idOf.Value(entity), cancellationToken))
                throw new ConflictException(DependentsMessage, ErrorCodes.HasActiveDependents);
        }

        private IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, string? sortBy, string? sortDirection)
        {
            var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            if (_sorting.Value.TryApply(sortBy, descending, query, out var sorted))
                return sorted;

            return descending ? query.OrderByDescending(IdSelector) : query.OrderBy(IdSelector);
        }
    }
}
