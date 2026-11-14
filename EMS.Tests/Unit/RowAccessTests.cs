using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;

namespace EMS.Tests.Unit
{
    /// <summary>The central row door: scope -> policies -> a query, and the 404 that never reveals a hidden record.</summary>
    public class RowAccessTests
    {
        public sealed class Doc { public long Id { get; set; } public long? Department { get; set; } public bool Secret { get; set; } }

        private sealed class DocContext : DbContext
        {
            public DocContext(DbContextOptions<DocContext> options) : base(options) { }
            public DbSet<Doc> Docs => Set<Doc>();
        }

        private sealed class FixedScope : IDataScopeAccessor { public DataScope Current { get; set; } = DataScope.System; }

        private sealed class DepartmentPolicy : RowPolicy<Doc>
        {
            public override string Name => "Department";
            public override string Describe(DataScope scope) => $"دپارتمان {scope.SupportingDepartmentId}";
            public override Expression<Func<Doc, bool>>? Filter(DataScope scope) { var d = scope.SupportingDepartmentId; return x => x.Department == d; }
        }

        private sealed class NotSecretPolicy : RowPolicy<Doc>
        {
            public override string Name => "NotSecret";
            public override string Describe(DataScope scope) => "فقط غیرمحرمانه";
            public override Expression<Func<Doc, bool>>? Filter(DataScope scope) => x => !x.Secret;
        }

        private sealed class OpenDocPolicy : OpenRowPolicy<Doc> { public override string Name => "OpenDoc"; }

        private sealed class ThrowingPolicy : RowPolicy<Doc>
        {
            public override string Name => "Throwing";
            public override string Describe(DataScope scope) => "";
            public override Expression<Func<Doc, bool>>? Filter(DataScope scope) => throw new InvalidOperationException("an unrestricted scope must not even ask the policies");
        }

        private sealed class RecordingSecurity : ISecurityEventLogger
        {
            public List<(string Type, object? Id, string Reason)> Denied { get; } = new();
            public void RowAccessDenied(string entityType, object? entityId, string scopeReason) => Denied.Add((entityType, entityId, scopeReason));
            public void LoginSucceeded(long userId, string userName) { }
            public void LoginFailed(string attemptedUserName, LoginFailureReason reason, long? userId = null) { }
            public void RefreshSucceeded(long userId) { }
            public void RefreshRejected(RefreshRejectionReason reason, long? userId = null) { }
            public void RefreshTokenRevoked(long? userId) { }
            public void UserRegistered(long userId, string userName) { }
            public void AccessDenied(string requiredPermission) { }
            public void AuthenticationFailed(string reason) { }
        }

        private static (RowAccess Access, DocContext Context, FixedScope Scope, RecordingSecurity Security) New(params Type[] policies)
        {
            var context = new DocContext(new DbContextOptionsBuilder<DocContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
            context.Docs.AddRange(
                new Doc { Id = 1, Department = 10 },
                new Doc { Id = 2, Department = 10, Secret = true },
                new Doc { Id = 3, Department = 20 },
                new Doc { Id = 4, Department = null });
            context.SaveChanges();

            var services = new ServiceCollection();
            foreach (var policy in policies)
            {
                services.AddSingleton(policy);
                services.AddSingleton(typeof(IRowPolicy<Doc>), sp => sp.GetRequiredService(policy));
                services.AddSingleton(typeof(IRowPolicyDescriptor), sp => sp.GetRequiredService(policy));
            }
            var scope = new FixedScope();
            var security = new RecordingSecurity();
            var access = new RowAccess(scope, services.BuildServiceProvider(), security, NullLogger<RowAccess>.Instance);
            return (access, context, scope, security);
        }

        private static DataScope Department(long id) => DataScope.Restricted(7, id, "آزمون");

        [Fact]
        public void An_unrestricted_scope_gets_the_rows_untouched_and_the_policies_are_not_even_asked()
        {
            var (access, context, scope, _) = New(typeof(ThrowingPolicy));
            scope.Current = DataScope.Unrestricted(7, 10, "همه");

            Assert.Equal(4, access.Apply(context.Docs).Count());
            Assert.Equal(4, New(typeof(DepartmentPolicy)).Access.Apply(New(typeof(DepartmentPolicy)).Context.Docs).Count());   // the system scope (no user) is unrestricted too
        }

        [Fact]
        public void A_restricted_scope_gets_the_condition_of_its_policy_and_several_policies_are_combined_with_AND()
        {
            var (access, context, scope, _) = New(typeof(DepartmentPolicy), typeof(NotSecretPolicy));

            scope.Current = Department(10);
            Assert.Equal(new long[] { 1 }, access.Apply(context.Docs).Select(d => d.Id).OrderBy(i => i).ToArray());     // department 10 AND not secret

            scope.Current = Department(20);
            Assert.Equal(new long[] { 3 }, access.Apply(context.Docs).Select(d => d.Id).ToArray());

            scope.Current = Department(99);
            Assert.Empty(access.Apply(context.Docs));
        }

        [Fact]
        public void A_policy_that_returns_no_condition_is_an_explicit_open_and_restricts_nothing()
        {
            var (access, context, scope, _) = New(typeof(OpenDocPolicy));
            scope.Current = Department(10);

            Assert.Equal(4, access.Apply(context.Docs).Count());
            Assert.False(access.Describe().Single().RestrictsThisScope);
        }

        [Fact]
        public void A_restricted_scope_with_no_policy_for_the_type_sees_nothing()
        {
            var (access, context, scope, _) = New();       // a type nobody wrote a policy for
            scope.Current = Department(10);

            Assert.Empty(access.Apply(context.Docs));
        }

        [Fact]
        public async Task A_record_that_exists_but_is_hidden_looks_exactly_like_one_that_does_not_exist_and_only_the_hidden_one_is_logged()
        {
            var (access, context, scope, security) = New(typeof(DepartmentPolicy));
            scope.Current = Department(10);

            await access.RequireVisibleAsync(context.Docs, d => d.Id == 1, "Doc", 1L, "یافت نشد.");                               // visible: no exception
            Assert.True(await access.IsVisibleAsync(context.Docs, d => d.Id == 1));

            var hidden = await Assert.ThrowsAsync<NotFoundException>(() => access.RequireVisibleAsync(context.Docs, d => d.Id == 3, "Doc", 3L, "یافت نشد."));
            var missing = await Assert.ThrowsAsync<NotFoundException>(() => access.RequireVisibleAsync(context.Docs, d => d.Id == 999, "Doc", 999L, "یافت نشد."));

            Assert.Equal(missing.Message, hidden.Message);          // the caller cannot tell them apart
            Assert.Equal(missing.Code, hidden.Code);
            Assert.Equal(ErrorCodes.NotFound, hidden.Code);

            var denied = Assert.Single(security.Denied);            // but the log can: the hidden one is recorded, the missing one is not
            Assert.Equal(("Doc", (object?)3L), (denied.Type, denied.Id));
            Assert.Contains("آزمون", denied.Reason);
        }

        [Fact]
        public async Task An_unrestricted_scope_sees_everything_and_a_missing_record_is_a_plain_404_without_a_security_entry()
        {
            var (access, context, scope, security) = New(typeof(DepartmentPolicy));
            scope.Current = DataScope.Unrestricted(7, null, "همه");

            await access.RequireVisibleAsync(context.Docs, d => d.Id == 3, "Doc", 3L, "یافت نشد.");
            await Assert.ThrowsAsync<NotFoundException>(() => access.RequireVisibleAsync(context.Docs, d => d.Id == 999, "Doc", 999L, "یافت نشد."));
            Assert.Empty(security.Denied);
        }

        [Fact]
        public void The_explanation_lists_every_policy_and_marks_the_ones_that_restrict_the_scope()
        {
            var (access, _, scope, _) = New(typeof(DepartmentPolicy), typeof(OpenDocPolicy));

            scope.Current = Department(10);
            var restricted = access.Describe();
            Assert.Equal(new[] { "Department", "OpenDoc" }, restricted.Select(i => i.Name));
            Assert.Equal(new[] { true, false }, restricted.Select(i => i.RestrictsThisScope));
            Assert.All(restricted, i => Assert.Equal("Doc", i.RowType));
            Assert.Equal("دپارتمان 10", restricted[0].Description);

            scope.Current = DataScope.Unrestricted(7, 10, "همه");
            Assert.All(access.Describe(), i => Assert.False(i.RestrictsThisScope));        // nobody is restricted, so nothing "restricts you"
        }
    }
}
