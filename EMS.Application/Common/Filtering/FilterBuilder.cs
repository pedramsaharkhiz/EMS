using System.Linq.Expressions;

namespace EMS.Application.Common.Filtering
{
    /// <summary>
    /// Declares the filters of a list, the way <c>SortBuilder</c> declares its sort columns: one line per filter, each applied
    /// only when its value was sent. The same builder serves the list and the export, so they can never disagree.
    /// <code>
    /// Filters = new FilterBuilder&lt;Equipment, EquipmentListRequestDto&gt;()
    ///     .Search(r => r.Search, x => x.YaasCode, x => x.SerialNumber)
    ///     .AnyOf(r => r.StatusIds, x => x.EquipmentStatusId)
    ///     .AtLeast(r => r.CreatedFrom, x => x.CreatedDateTime);
    /// </code>
    /// A filter that needs more than these building blocks uses <see cref="Add"/> with its own expression.
    /// Values are passed to the database as parameters, never concatenated.
    /// </summary>
    public sealed class FilterBuilder<TEntity, TRequest>
    {
        private readonly List<Func<TRequest, Expression<Func<TEntity, bool>>?>> _filters = new();

        /// <summary>A custom filter: return the predicate to apply, or null when the request does not ask for it.</summary>
        public FilterBuilder<TEntity, TRequest> Add(Func<TRequest, Expression<Func<TEntity, bool>>?> filter)
        {
            _filters.Add(filter);
            return this;
        }

        /// <summary>Property equals the value (skipped when the value is null).</summary>
        public FilterBuilder<TEntity, TRequest> Eq<TValue>(Func<TRequest, TValue?> value, Expression<Func<TEntity, TValue>> property)
            where TValue : struct => EqCore(value, property);

        /// <inheritdoc cref="Eq{TValue}(Func{TRequest, TValue?}, Expression{Func{TEntity, TValue}})"/>
        public FilterBuilder<TEntity, TRequest> Eq<TValue>(Func<TRequest, TValue?> value, Expression<Func<TEntity, TValue?>> property)
            where TValue : struct => EqCore(value, property);

        /// <summary>Property is one of the values (skipped when null or empty). For multi-select filters: <c>?statusId=1&amp;statusId=2</c>.</summary>
        public FilterBuilder<TEntity, TRequest> AnyOf<TValue>(Func<TRequest, IReadOnlyCollection<TValue>?> values, Expression<Func<TEntity, TValue>> property)
            where TValue : struct => AnyOfCore(values, property);

        /// <inheritdoc cref="AnyOf{TValue}(Func{TRequest, IReadOnlyCollection{TValue}?}, Expression{Func{TEntity, TValue}})"/>
        public FilterBuilder<TEntity, TRequest> AnyOf<TValue>(Func<TRequest, IReadOnlyCollection<TValue>?> values, Expression<Func<TEntity, TValue?>> property)
            where TValue : struct => AnyOfCore(values, property);

        /// <summary>Text property contains the (trimmed) text; skipped when blank.</summary>
        public FilterBuilder<TEntity, TRequest> Contains(Func<TRequest, string?> text, Expression<Func<TEntity, string?>> property) =>
            Search(text, property);

        /// <summary>Any of the text properties contains the (trimmed) text; skipped when blank.</summary>
        public FilterBuilder<TEntity, TRequest> Search(Func<TRequest, string?> text, params Expression<Func<TEntity, string?>>[] properties)
        {
            if (properties.Length == 0)
                throw new ArgumentException("At least one property is required.", nameof(properties));

            return Add(request =>
            {
                var term = text(request)?.Trim();
                if (string.IsNullOrEmpty(term))
                    return null;

                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var contains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
                var termExpression = Parameterized(term);

                Expression? combined = null;
                foreach (var property in properties)
                {
                    var body = Rebind(property, parameter);
                    // "IS NOT NULL AND LIKE": a null column never matches (and the predicate also runs against plain objects in tests).
                    var match = Expression.AndAlso(
                        Expression.NotEqual(body, Expression.Constant(null, typeof(string))),
                        Expression.Call(body, contains, termExpression));
                    combined = combined is null ? match : Expression.OrElse(combined, match);
                }

                return Expression.Lambda<Func<TEntity, bool>>(combined!, parameter);
            });
        }

        /// <summary>Property is greater than or equal to the value (skipped when null). Typically the start of a date range.</summary>
        public FilterBuilder<TEntity, TRequest> AtLeast<TValue>(Func<TRequest, TValue?> value, Expression<Func<TEntity, TValue>> property)
            where TValue : struct => CompareCore(value, property, Expression.GreaterThanOrEqual);

        /// <inheritdoc cref="AtLeast{TValue}(Func{TRequest, TValue?}, Expression{Func{TEntity, TValue}})"/>
        public FilterBuilder<TEntity, TRequest> AtLeast<TValue>(Func<TRequest, TValue?> value, Expression<Func<TEntity, TValue?>> property)
            where TValue : struct => CompareCore(value, property, Expression.GreaterThanOrEqual);

        /// <summary>Property is less than or equal to the value (skipped when null). The end of a range is inclusive as sent.</summary>
        public FilterBuilder<TEntity, TRequest> AtMost<TValue>(Func<TRequest, TValue?> value, Expression<Func<TEntity, TValue>> property)
            where TValue : struct => CompareCore(value, property, Expression.LessThanOrEqual);

        /// <inheritdoc cref="AtMost{TValue}(Func{TRequest, TValue?}, Expression{Func{TEntity, TValue}})"/>
        public FilterBuilder<TEntity, TRequest> AtMost<TValue>(Func<TRequest, TValue?> value, Expression<Func<TEntity, TValue?>> property)
            where TValue : struct => CompareCore(value, property, Expression.LessThanOrEqual);

        /// <summary>Applies every filter whose value was sent.</summary>
        public IQueryable<TEntity> Apply(IQueryable<TEntity> query, TRequest request)
        {
            foreach (var filter in _filters)
            {
                if (filter(request) is { } predicate)
                    query = query.Where(predicate);
            }

            return query;
        }

        // ------------------------------------------------------------------------------------------------

        private FilterBuilder<TEntity, TRequest> EqCore<TValue>(Func<TRequest, TValue?> value, LambdaExpression property)
            where TValue : struct =>
            Add(request =>
            {
                if (value(request) is not { } v)
                    return null;

                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var body = Rebind(property, parameter);
                return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(body, Parameterized(v, body.Type)), parameter);
            });

        private FilterBuilder<TEntity, TRequest> AnyOfCore<TValue>(Func<TRequest, IReadOnlyCollection<TValue>?> values, LambdaExpression property)
            where TValue : struct =>
            Add(request =>
            {
                var list = values(request);
                if (list is null || list.Count == 0)
                    return null;

                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var body = Rebind(property, parameter);

                // A nullable column is compared as TValue?, a plain one as TValue: the array has to have the column's type.
                var nullable = Nullable.GetUnderlyingType(body.Type) is not null;
                var elementType = nullable ? typeof(TValue?) : typeof(TValue);
                var array = Array.CreateInstance(elementType, list.Count);
                var index = 0;
                foreach (var item in list)
                    array.SetValue(item, index++);

                var contains = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                    .MakeGenericMethod(elementType);

                return Expression.Lambda<Func<TEntity, bool>>(Expression.Call(contains, Parameterized(array, array.GetType()), body), parameter);
            });

        private FilterBuilder<TEntity, TRequest> CompareCore<TValue>(
            Func<TRequest, TValue?> value, LambdaExpression property, Func<Expression, Expression, BinaryExpression> compare)
            where TValue : struct =>
            Add(request =>
            {
                if (value(request) is not { } v)
                    return null;

                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var body = Rebind(property, parameter);
                // Compared as nullable on both sides so a nullable column simply never matches a missing value.
                var nullableType = typeof(TValue?);
                var left = body.Type == nullableType ? body : Expression.Convert(body, nullableType);
                return Expression.Lambda<Func<TEntity, bool>>(compare(left, Parameterized(v, nullableType)), parameter);
            });

        /// <summary>The selector's body with its own parameter replaced by <paramref name="parameter"/>.</summary>
        private static Expression Rebind(LambdaExpression selector, ParameterExpression parameter) =>
            new ParameterReplacer(selector.Parameters[0], parameter).Visit(selector.Body)!;

        /// <summary>
        /// A value as a query parameter: read from a field of a closure object, which EF turns into a SQL parameter
        /// (a constant would be inlined into the SQL text and defeat the query plan cache).
        /// </summary>
        private static Expression Parameterized(object value, Type? asType = null)
        {
            var type = value.GetType();
            var holder = Activator.CreateInstance(typeof(FilterValueHolder<>).MakeGenericType(type), value)!;
            Expression access = Expression.Field(Expression.Constant(holder), nameof(FilterValueHolder<int>.Value));
            return asType is not null && asType != type ? Expression.Convert(access, asType) : access;
        }

        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ParameterReplacer(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : node;
        }
    }

    /// <summary>Carries a filter value in a field, so the expression tree reads it like a captured variable (a SQL parameter).</summary>
    internal sealed class FilterValueHolder<T>
    {
        public readonly T Value;
        public FilterValueHolder(T value) => Value = value;
    }
}
