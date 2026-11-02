using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IRepository<T> where T : class
    {
        //Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

        IQueryable<T> Query(bool asNoTracking = true);

        Task<List<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        void Update(T entity);

        /// <summary>Makes the next save fail if the row's ModifiedDateTime in the database is not <paramref name="expected"/>.</summary>
        void SetExpectedModifiedDateTime(T entity, DateTime? expected);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}
