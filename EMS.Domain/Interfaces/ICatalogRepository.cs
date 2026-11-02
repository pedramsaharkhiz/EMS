using EMS.Domain.Entities;

namespace EMS.Domain.Interfaces
{
    public interface ICatalogRepository<T> : IRepository<T> where T : BaseEntity, ICatalogEntity
    {
        /// <summary>Loads a record by its primary key, active or not (a deactivated record can still be viewed and restored).</summary>
        Task<T?> GetByIdAsync(long id, bool asNoTracking = true, CancellationToken cancellationToken = default);

        /// <summary>Read-only query over the active (or, with false, the deactivated) records.</summary>
        IQueryable<T> QueryByActive(bool active);

        /// <summary>Is the title used by another ACTIVE record? (Deactivated records do not reserve their title.)</summary>
        Task<bool> ExistsByTitleAsync(string title, long? exceptId = null, CancellationToken cancellationToken = default);

        /// <summary>True when active records still depend on this one, so it must not be deactivated.</summary>
        Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default);
    }
}
