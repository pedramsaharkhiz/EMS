using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    /// <summary>
    /// Generic data access shared by all catalog entities. A concrete repository only supplies
    /// <see cref="HasActiveDependentsAsync"/>, because what depends on a record differs per entity.
    /// </summary>
    public abstract class CatalogRepository<T> : Repository<T>, ICatalogRepository<T>
        where T : BaseEntity, ICatalogEntity
    {
        private readonly string _keyName;

        protected CatalogRepository(EMSDbContext context) : base(context)
        {
            // Every catalog has a single long primary key (BrandId, EquipmentStatusId, ...): ask EF for its name.
            var entityType = context.Model.FindEntityType(typeof(T))!;
            _keyName = entityType.FindPrimaryKey()!.Properties.Single().Name;

            // The queries below rely on the soft-delete filter instead of repeating "IsActive" - fail fast if it is missing.
            if (!entityType.GetDeclaredQueryFilters().Any(filter => filter.Key == SoftDelete.FilterName))
                throw new InvalidOperationException(
                    $"{typeof(T).Name} is a catalog entity, so its EF configuration must call builder.HasSoftDeleteFilter().");
        }

        public async Task<T?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            // A deactivated record can still be viewed and restored, so the soft-delete filter is lifted here.
            IQueryable<T> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            return await query.IncludingInactive().FirstOrDefaultAsync(x => EF.Property<long>(x, _keyName) == id, cancellationToken);
        }

        public IQueryable<T> QueryByActive(bool active) =>
            active
                ? _dbSet.AsNoTracking()                                                       // the filter keeps only active records
                : _dbSet.AsNoTracking().IncludingInactive().Where(x => !x.IsActive);          // lift it to see the deactivated ones

        public async Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var normalized = title.Trim().ToLower();

            // Only active records reserve a title: a deactivated record must not block its reuse
            // (the soft-delete filter hides deactivated records from this query).
            var query = _dbSet.Where(x => x.Title.ToLower() == normalized);
            if (exceptId.HasValue)
            {
                var except = exceptId.Value;
                query = query.Where(x => EF.Property<long>(x, _keyName) != except);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public abstract Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default);
    }
}
