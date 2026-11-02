using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence
{
    /// <summary>
    /// Soft delete as a global query filter: a record with <c>IsActive = false</c> is invisible to every query on that
    /// entity unless the query explicitly asks for it. This replaces repeating <c>.Where(x =&gt; x.IsActive)</c> in every
    /// query (a forgotten filter used to be a bug).
    ///
    /// The filter is named so it can be lifted on its own without disabling other filters.
    ///
    /// Opt-in per entity (call <see cref="HasSoftDeleteFilter{T}"/> in its EF configuration). It is deliberately NOT applied
    /// to every entity: when an entity that is *not* filtered references one that is (e.g. Equipment -> EquipmentStatus),
    /// an <c>Include</c> would drop rows whose referenced record was deactivated. Such queries must use
    /// <see cref="IncludingInactive{T}"/>. See docs/CATALOGS.md.
    /// </summary>
    public static class SoftDelete
    {
        public const string FilterName = "SoftDelete";

        /// <summary>Hides deactivated records from all queries on this entity.</summary>
        public static EntityTypeBuilder<T> HasSoftDeleteFilter<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity =>
            builder.HasQueryFilter(FilterName, x => x.IsActive);

        /// <summary>Lifts the soft-delete filter for this query (and for everything it includes).</summary>
        public static IQueryable<T> IncludingInactive<T>(this IQueryable<T> query) where T : class =>
            query.IgnoreQueryFilters(new[] { FilterName });
    }
}
