using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace EMS.Application.Common.DataAccess
{
    /// <summary>The implementation of <see cref="IRowAccess"/>: policies are found by row type, the scope comes from <see cref="IDataScopeAccessor"/>.</summary>
    public sealed class RowAccess : IRowAccess
    {
        private readonly IDataScopeAccessor _scope;
        private readonly IServiceProvider _services;
        private readonly ISecurityEventLogger _security;
        private readonly ILogger<RowAccess> _logger;

        public RowAccess(IDataScopeAccessor scope, IServiceProvider services, ISecurityEventLogger security, ILogger<RowAccess> logger)
        {
            _scope = scope;
            _services = services;
            _security = security;
            _logger = logger;
        }

        public IQueryable<TRow> Apply<TRow>(IQueryable<TRow> rows)
        {
            var scope = _scope.Current;
            if (scope.IsUnrestricted)
                return rows;

            var policies = _services.GetServices<IRowPolicy<TRow>>().ToList();
            if (policies.Count == 0)
            {
                // A type that is read through the door but has no policy: safe by default, and loud.
                _logger.LogError("Row access: no policy is registered for {RowType}; user {UserId} (restricted: {Reason}) sees nothing of it", typeof(TRow).Name, scope.UserId, scope.Reason);
                return rows.Where(_ => false);
            }

            Expression<Func<TRow, bool>>? combined = null;
            foreach (var policy in policies)
            {
                var filter = policy.Filter(scope);
                if (filter is not null)
                    combined = combined is null ? filter : And(combined, filter);
            }

            if (combined is null)
                return rows;

            _logger.LogDebug("Row access: {Policies} restrict {RowType} for user {UserId} ({Reason})", string.Join(",", policies.Select(p => p.Name)), typeof(TRow).Name, scope.UserId, scope.Reason);
            return rows.Where(combined);
        }

        public DataScope Scope => _scope.Current;

        public bool IsVisible<TRow>(TRow candidate)
        {
            var scope = _scope.Current;
            if (scope.IsUnrestricted)
                return true;

            var policies = _services.GetServices<IRowPolicy<TRow>>().ToList();
            if (policies.Count == 0)
                return false;

            return policies.Select(p => p.Filter(scope)).Where(f => f is not null).All(f => f!.Compile()(candidate));
        }

        public async Task<bool> IsVisibleAsync<TRow>(IQueryable<TRow> allRows, Expression<Func<TRow, bool>> isTheRecord, CancellationToken cancellationToken = default) =>
            await Apply(allRows).AnyAsync(isTheRecord, cancellationToken);

        public async Task RequireVisibleAsync<TRow>(IQueryable<TRow> allRows, Expression<Func<TRow, bool>> isTheRecord, string entityType, object? entityId, string notFoundMessage, CancellationToken cancellationToken = default, string? errorCode = null)
        {
            if (await IsVisibleAsync(allRows, isTheRecord, cancellationToken))
                return;

            // Only now (the rare path) do we ask whether it exists, so that the log can tell "hidden from him" from "there is no such record".
            if (_scope.Current.IsUnrestricted == false && await allRows.AnyAsync(isTheRecord, cancellationToken))
                _security.RowAccessDenied(entityType, entityId, _scope.Current.Reason);

            throw new NotFoundException(notFoundMessage, errorCode ?? ErrorCodes.NotFound);
        }

        public IReadOnlyList<RowPolicyInfo> Describe()
        {
            var scope = _scope.Current;
            return _services.GetServices<IRowPolicyDescriptor>()
                .Select(p => new RowPolicyInfo(p.Name, p.RowType.Name, p.Describe(scope), !scope.IsUnrestricted && p.Restricts(scope)))
                .OrderBy(i => i.RowType).ThenBy(i => i.Name).ToList();
        }

        private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            var parameter = left.Parameters[0];
            var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body)!;
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody), parameter);
        }

        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;
            public ParameterReplacer(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }
            protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : base.VisitParameter(node);
        }
    }
}
