using System.Linq.Expressions;

namespace EMS.Application.Services
{
    /// <summary>Declares which columns a list can be sorted by (the value of the <c>sortBy</c> query parameter).</summary>
    public sealed class SortBuilder<TEntity>
    {
        private readonly Dictionary<string, Func<IQueryable<TEntity>, bool, IQueryable<TEntity>>> _columns = new();

        public SortBuilder<TEntity> By<TKey>(string name, Expression<Func<TEntity, TKey>> key)
        {
            _columns[name.ToLowerInvariant()] = (query, descending) =>
                descending ? query.OrderByDescending(key) : query.OrderBy(key);
            return this;
        }

        internal bool TryApply(string? sortBy, bool descending, IQueryable<TEntity> query, out IQueryable<TEntity> sorted)
        {
            sorted = query;
            if (string.IsNullOrWhiteSpace(sortBy) || !_columns.TryGetValue(sortBy.Trim().ToLowerInvariant(), out var apply))
                return false;

            sorted = apply(query, descending);
            return true;
        }
    }
}
