using EMS.Application.Common.Filtering;

namespace EMS.Tests.Unit
{
    public class FilterBuilderTests
    {
        private sealed record Item(long Id, long? OptionalId, string? Name, string? Code, DateTime Created, decimal? Price);

        private sealed class Request
        {
            public long? Id { get; set; }
            public long? OptionalId { get; set; }
            public long[]? Ids { get; set; }
            public long[]? OptionalIds { get; set; }
            public string? Search { get; set; }
            public string? Name { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public decimal? MinPrice { get; set; }
        }

        private static readonly Item[] Items =
        {
            new(1, 10, "Router Alpha", "A-1", new DateTime(2025, 1, 1), 5m),
            new(2, 20, "Switch Beta", "B-2", new DateTime(2025, 2, 1), 15m),
            new(3, null, "Router Gamma", null, new DateTime(2025, 3, 1), null),
            new(4, 10, null, "G-4", new DateTime(2025, 4, 1), 25m),
        };

        private static FilterBuilder<Item, Request> Builder() => new FilterBuilder<Item, Request>()
            .Eq(r => r.Id, x => x.Id)
            .Eq(r => r.OptionalId, x => x.OptionalId)
            .AnyOf(r => r.Ids, x => x.Id)
            .AnyOf(r => r.OptionalIds, x => x.OptionalId)
            .Search(r => r.Search, x => x.Name, x => x.Code)
            .Contains(r => r.Name, x => x.Name)
            .AtLeast(r => r.From, x => x.Created)
            .AtMost(r => r.To, x => x.Created)
            .AtLeast(r => r.MinPrice, x => x.Price);

        private static long[] Run(Request request) =>
            Builder().Apply(Items.AsQueryable(), request).Select(x => x.Id).OrderBy(x => x).ToArray();

        [Fact]
        public void An_empty_request_applies_no_filter()
        {
            Assert.Equal(new long[] { 1, 2, 3, 4 }, Run(new Request()));
            Assert.Equal(new long[] { 1, 2, 3, 4 }, Run(new Request { Ids = Array.Empty<long>(), Search = "  " }));
        }

        [Fact]
        public void Eq_matches_plain_and_nullable_columns()
        {
            Assert.Equal(new long[] { 2 }, Run(new Request { Id = 2 }));
            Assert.Equal(new long[] { 1, 4 }, Run(new Request { OptionalId = 10 }));
        }

        [Fact]
        public void AnyOf_matches_any_of_the_values_on_plain_and_nullable_columns()
        {
            Assert.Equal(new long[] { 1, 3 }, Run(new Request { Ids = new long[] { 1, 3, 99 } }));
            Assert.Equal(new long[] { 1, 2, 4 }, Run(new Request { OptionalIds = new long[] { 10, 20 } }));
        }

        [Fact]
        public void Search_looks_in_every_property_and_ignores_null_columns_and_surrounding_spaces()
        {
            Assert.Equal(new long[] { 1, 3 }, Run(new Request { Search = " Router " }));
            Assert.Equal(new long[] { 4 }, Run(new Request { Search = "G-4" }));            // matched in Code, whose Name is null
            Assert.Empty(Run(new Request { Search = "nothing" }));
        }

        [Fact]
        public void Contains_filters_one_property()
        {
            Assert.Equal(new long[] { 2 }, Run(new Request { Name = "Beta" }));
        }

        [Fact]
        public void Range_filters_are_inclusive_and_combine_with_the_other_filters()
        {
            Assert.Equal(new long[] { 2, 3 }, Run(new Request { From = new DateTime(2025, 2, 1), To = new DateTime(2025, 3, 1) }));
            Assert.Equal(new long[] { 3 }, Run(new Request { From = new DateTime(2025, 2, 1), Search = "Router" }));
            Assert.Equal(new long[] { 2, 4 }, Run(new Request { MinPrice = 15m }));         // a null price never matches
        }

        [Fact]
        public void Values_become_query_parameters_not_constants()
        {
            var builder = Builder();
            var query = builder.Apply(Items.AsQueryable(), new Request { Id = 2, Ids = new long[] { 1, 2 }, Search = "a" });
            var text = query.Expression.ToString();

            // a FilterValueHolder<T>.Value field access (a closure) rather than the literal values inlined into the tree
            Assert.Contains("FilterValueHolder", text);
        }
    }
}
