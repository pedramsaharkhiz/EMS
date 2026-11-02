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
            _keyName = context.Model.FindEntityType(typeof(T))!.FindPrimaryKey()!.Properties.Single().Name;
        }

        public async Task<T?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            return await query.FirstOrDefaultAsync(x => EF.Property<long>(x, _keyName) == id, cancellationToken);
        }

        public IQueryable<T> QueryByActive(bool active) =>
            _dbSet.AsNoTracking().Where(x => x.IsActive == active);

        public async Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default)
        {
            var normalized = title.Trim().ToLower();

            // Only active records reserve a title: a deactivated record must not block its reuse.
            var query = _dbSet.Where(x => x.IsActive && x.Title.ToLower() == normalized);
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
