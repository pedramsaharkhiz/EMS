using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Repositories;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// A row policy is an expression over a read model, and the read models are heavy. The in-memory provider cannot tell whether SQL Server can translate the
    /// condition a policy adds, so this asks the real SQL Server provider (without connecting) to translate the very rows the lists read with a policy applied.
    /// Every policy written in phase 2 should get a line here.
    /// </summary>
    public class RowPolicySqlTranslationTests
    {
        private sealed class NoUser : ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "t";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private sealed class FixedScope : IDataScopeAccessor { public DataScope Current { get; } = DataScope.Restricted(1, 5, "آزمون"); }

        private sealed class NoSecurityLog : ISecurityEventLogger
        {
            public void RowAccessDenied(string entityType, object? entityId, string scopeReason) { }
            public void LoginSucceeded(long userId, string userName) { }
            public void LoginFailed(string attemptedUserName, LoginFailureReason reason, long? userId = null) { }
            public void RefreshSucceeded(long userId) { }
            public void RefreshRejected(RefreshRejectionReason reason, long? userId = null) { }
            public void RefreshTokenRevoked(long? userId) { }
            public void UserRegistered(long userId, string userName) { }
            public void AccessDenied(string requiredPermission) { }
            public void AuthenticationFailed(string reason) { }
        }

        private static EMSDbContext NewContext() =>
            new(new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer("Server=localhost;Database=EmsTranslationOnly;Trusted_Connection=True;TrustServerCertificate=True").Options,
                new AuditSaveChangesInterceptor(new NoUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                    Microsoft.Extensions.Options.Options.Create(new AuditTrailOptions()),
                    NullLogger<AuditSaveChangesInterceptor>.Instance));

        private static RowAccess NewAccess()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IRowPolicy<EMS.Domain.ReadModels.FailureReportRow>, TestFailureReportDepartmentPolicy>();
            services.AddSingleton<IRowPolicy<EMS.Domain.ReadModels.EquipmentRow>, TestEquipmentDepartmentPolicy>();
            return new RowAccess(new FixedScope(), services.BuildServiceProvider(), new NoSecurityLog(), NullLogger<RowAccess>.Instance);
        }

        [Fact]
        public void A_policy_on_the_failure_report_rows_translates_to_sql_for_the_list_and_for_a_count()
        {
            using var context = NewContext();
            var rows = NewAccess().Apply(new FailureReportRepository(context).QueryRows(true));

            var sql = rows.OrderBy(r => r.Id).ToQueryString();
            Assert.Contains("[SupportingDepartmentId]", sql);
            Assert.Contains("= @", sql);                                   // the department is a parameter, never concatenated text
            Assert.Contains("COUNT(", rows.GroupBy(r => r.StatusId).Select(g => new { g.Key, Count = g.Count() }).ToQueryString());
        }

        [Fact]
        public void A_policy_with_an_or_on_the_equipment_rows_translates_to_sql()
        {
            using var context = NewContext();
            var sql = NewAccess().Apply(new EquipmentRepository(context).QueryRows(true)).OrderBy(r => r.Id).ToQueryString();

            Assert.Contains(" OR ", sql);
            Assert.Contains("[SupportingDepartmentId]", sql);
        }

        [Fact]
        public void A_restricted_scope_with_no_policy_translates_to_a_condition_that_matches_no_row()
        {
            using var context = NewContext();
            var none = new RowAccess(new FixedScope(), new ServiceCollection().BuildServiceProvider(), new NoSecurityLog(), NullLogger<RowAccess>.Instance);

            Assert.Matches(@"0 = 1|1 = 0", none.Apply(new FailureReportRepository(context).QueryRows(true)).ToQueryString());
        }
    }
}
