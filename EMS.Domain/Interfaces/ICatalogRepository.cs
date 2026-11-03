using EMS.Domain.Entities;
using System.Linq.Expressions;

namespace EMS.Domain.Interfaces
{
    public interface ICatalogRepository<T> : IRepository<T> where T : BaseEntity, ICatalogEntity
    {
        /// <summary>Loads a record by its primary key, active or not (a deactivated record can still be viewed and restored).</summary>
        Task<T?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default);

        /// <summary>Read-only query over the active (or, with false, the deactivated) records.</summary>
        IQueryable<T> QueryByActive(bool active);

        /// <summary>
        /// Is the title used by another ACTIVE record? (Deactivated records do not reserve their title.)
        /// <paramref name="scope"/> narrows the comparison to a group, e.g. "the same city" when titles only have to be unique
        /// within a parent; null means unique across the whole table.
        /// </summary>
        Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, Expression<Func<T, bool>>? scope = null, CancellationToken cancellationToken = default);

        /// <summary>True when active records still depend on this one, so it must not be deactivated.</summary>
        Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default);
    }
}
