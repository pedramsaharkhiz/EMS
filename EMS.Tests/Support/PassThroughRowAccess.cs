using EMS.Application.Common.DataAccess;
using System.Linq.Expressions;

namespace EMS.Tests.Support
{
    /// <summary>A row door that restricts nothing, for tests that build a service by hand just to look at its SQL.</summary>
    public sealed class PassThroughRowAccess : IRowAccess
    {
        public DataScope Scope => DataScope.System;
        public bool IsVisible<TRow>(TRow candidate) => true;
        public IQueryable<TRow> Apply<TRow>(IQueryable<TRow> rows) => rows;
        public Task<bool> IsVisibleAsync<TRow>(IQueryable<TRow> allRows, Expression<Func<TRow, bool>> isTheRecord, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task RequireVisibleAsync<TRow>(IQueryable<TRow> allRows, Expression<Func<TRow, bool>> isTheRecord, string entityType, object? entityId, string notFoundMessage, CancellationToken cancellationToken = default, string? errorCode = null) => Task.CompletedTask;
        public IReadOnlyList<RowPolicyInfo> Describe() => Array.Empty<RowPolicyInfo>();
    }
}
