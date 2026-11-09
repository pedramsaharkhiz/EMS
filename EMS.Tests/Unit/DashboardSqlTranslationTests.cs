using EMS.Application.Common.Interfaces;
using EMS.Domain;
using EMS.Domain.Enums;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// The dashboard counts run <c>GROUP BY</c> over the same row sources as the lists, and those rows are heavy (a dozen correlated subqueries
    /// for the people and the joins for titles). The in-memory provider cannot show what SQL Server would receive. This translates the very
    /// same query shapes with the real SQL Server provider (without connecting) and requires that EF drops the columns a count never reads:
    /// no user names, no titles. If it ever stops doing so, a card would read the whole row per record and this test says so.
    /// Keep the shapes in sync with the three <c>Get...Async</c> dashboard methods of the services.
    /// </summary>
    public class DashboardSqlTranslationTests
    {
        private sealed class NoUser : ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "t";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private static EMSDbContext NewContext() =>
            new(new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer("Server=localhost;Database=EmsTranslationOnly;Trusted_Connection=True;TrustServerCertificate=True").Options,
                new AuditSaveChangesInterceptor(new NoUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                    Microsoft.Extensions.Options.Options.Create(new AuditTrailOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditSaveChangesInterceptor>.Instance));

        private static void AssertLean(string sql, string what)
        {
            Assert.Contains("GROUP BY", sql);
            foreach (var heavy in new[] { "[FirstName]", "[PersonnelCode]", "[Title]" })
                Assert.True(!sql.Contains(heavy), $"{what}: the count query still reads {heavy}:\n{sql}");
        }

        [Fact]
        public void The_equipment_counts_per_status_and_per_owner_read_no_titles_or_people()
        {
            using var context = NewContext();
            var rows = new EquipmentRepository(context).QueryRows(true);

            // control: the full row does read the people, so the "lean" checks below are not vacuous
            Assert.Contains("[FirstName]", rows.ToQueryString());

            AssertLean(rows.GroupBy(r => r.StatusId).Select(g => new { Id = g.Key, Count = g.Count() }).ToQueryString(), "equipment by status");
            AssertLean(rows.GroupBy(r => r.Owner).Select(g => new { Owner = g.Key, Count = g.Count() }).ToQueryString(), "equipment by owner");
        }

        [Fact]
        public void The_failure_report_counts_read_no_titles_or_people_and_the_open_assignment_is_one_exists_check()
        {
            using var context = NewContext();
            var rows = new FailureReportRepository(context).QueryRows(true);
            var closed = FailureReportStatusCatalog.IdOf(FailureReportStatusCode.Closed);

            AssertLean(rows.GroupBy(r => r.StatusId).Select(g => new { Id = g.Key, Count = g.Count() }).ToQueryString(), "reports by status");
            AssertLean(rows.Where(r => r.StatusId != closed).GroupBy(r => r.PriorityId).Select(g => new { Id = g.Key, Count = g.Count() }).ToQueryString(), "unresolved by priority");

            // the count without an assignment is a COUNT with an EXISTS, not a row-by-row read
            var sql = rows.Where(r => r.StatusId != closed && !r.HasOpenAssignment).Select(r => r.Id).ToQueryString();
            Assert.Contains("NOT EXISTS", sql);
            foreach (var heavy in new[] { "[FirstName]", "[PersonnelCode]" })
                Assert.DoesNotContain(heavy, sql);
        }

        [Fact]
        public void The_assignment_counts_translate_and_read_no_titles()
        {
            using var context = NewContext();
            var rows = new TechnicianAssignmentRepository(context).QueryRows();
            var now = DateTime.UtcNow;
            var open = TechnicianAssignmentCatalog.OpenIds;

            AssertLean(rows.GroupBy(r => r.StatusId).Select(g => new { Id = g.Key, Count = g.Count() }).ToQueryString(), "assignments by status");
            var overdue = rows.Where(r => r.DeadlineDateTime != null && r.DeadlineDateTime < now && open.Contains(r.StatusId)).Select(r => r.Id).ToQueryString();
            Assert.Contains("[DeadlineDateTime]", overdue);
        }
    }
}
