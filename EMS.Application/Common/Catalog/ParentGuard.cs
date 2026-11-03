using EMS.Application.Common.Exceptions;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using System.Linq.Expressions;

namespace EMS.Application.Common.Catalog
{
    /// <summary>The check every record that points at another record shares: it must exist and (usually) be active.</summary>
    public static class ParentGuard
    {
        /// <summary>
        /// Requires the referenced record to exist (404 <c>parent_not_found</c>) and, when <paramref name="mustBeActive"/>,
        /// to be active (400 <c>parent_inactive</c>). <paramref name="label"/> is the Persian name used in the message.
        /// </summary>
        public static async Task RequireAsync<TParent>(
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
    }
}
