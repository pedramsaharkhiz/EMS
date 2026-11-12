using System.Linq.Expressions;

namespace EMS.Application.Common.DataAccess
{
    /// <summary>What the explanation screen needs from a policy without knowing its row type.</summary>
    public interface IRowPolicyDescriptor
    {
        /// <summary>A short stable name for the log and the explanation screen, e.g. <c>FailureReport</c>.</summary>
        string Name { get; }

        /// <summary>The read model the policy protects.</summary>
        Type RowType { get; }

        /// <summary>The rule in words for <paramref name="scope"/>: shown to the administrator who asks "why can't he see this?".</summary>
        string Describe(DataScope scope);

        /// <summary>Does the policy actually restrict <paramref name="scope"/>?</summary>
        bool Restricts(DataScope scope);
    }

    /// <summary>
    /// The rule that says which rows of one read model (<typeparamref name="TRow"/>) a scope may see. ONE definition serves every use: the
    /// list, the Excel export, the dashboard, the detail by id and the write checks all ask the same expression, so they can never disagree.
    /// Write a policy by deriving from <see cref="RowPolicy{TRow}"/> (one class per row type; they are discovered automatically; several policies
    /// for the same type are combined with AND).
    /// </summary>
    public interface IRowPolicy<TRow> : IRowPolicyDescriptor
    {
        /// <summary>
        /// The condition a row must meet for <paramref name="scope"/>; it must be translatable to SQL. <c>null</c> = this policy deliberately puts no
        /// restriction on the type (an explicit "open", not a forgotten one). Only called for a restricted scope.
        /// </summary>
        Expression<Func<TRow, bool>>? Filter(DataScope scope);
    }

    public abstract class RowPolicy<TRow> : IRowPolicy<TRow>
    {
        public abstract string Name { get; }
        public Type RowType => typeof(TRow);
        public abstract Expression<Func<TRow, bool>>? Filter(DataScope scope);
        public abstract string Describe(DataScope scope);
        public bool Restricts(DataScope scope) => Filter(scope) is not null;
    }

    /// <summary>A policy that puts no restriction on the type. Every protected type starts with one: the type is registered and visible, the rule comes later.</summary>
    public abstract class OpenRowPolicy<TRow> : RowPolicy<TRow>
    {
        public override Expression<Func<TRow, bool>>? Filter(DataScope scope) => null;
        public override string Describe(DataScope scope) => "بدون محدودیت (قاعده‌ی دید برای این نوع هنوز تعریف نشده است)";
    }

    /// <summary>One policy as the explanation screen shows it.</summary>
    public sealed record RowPolicyInfo(string Name, string RowType, string Description, bool RestrictsThisScope);

    /// <summary>
    /// The single door every read and write of protected rows goes through. A service never filters by user itself: it asks this.
    /// </summary>
    public interface IRowAccess
    {
        /// <summary>
        /// The rows the current scope may see. An unrestricted scope gets <paramref name="rows"/> unchanged; a restricted one gets the policies' condition added.
        /// A restricted scope with NO policy registered for the type sees NOTHING (fail closed) and the log says so.
        /// </summary>
        IQueryable<TRow> Apply<TRow>(IQueryable<TRow> rows);

        /// <summary>
        /// For a record reached by its id (detail, update, delete, move, attachments): throws <c>404 not_found</c> unless the record exists AND is visible.
        /// A record that exists but is hidden is refused with the very same 404 (its existence must not leak) and the refusal is written to the security log.
        /// </summary>
        Task RequireVisibleAsync<TRow>(IQueryable<TRow> allRows, Expression<Func<TRow, bool>> isTheRecord, string entityType, object? entityId, string notFoundMessage, CancellationToken cancellationToken = default);

        /// <summary>The same question without the exception.</summary>
        Task<bool> IsVisibleAsync<TRow>(IQueryable<TRow> allRows, Expression<Func<TRow, bool>> isTheRecord, CancellationToken cancellationToken = default);

        /// <summary>Every registered policy with its explanation for the current scope.</summary>
        IReadOnlyList<RowPolicyInfo> Describe();
    }
}
