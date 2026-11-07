using EMS.Infrastructure.Persistence;
using EMS.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// The start-up migration against a real SQL Server (the first test that applies EVERY migration, in order, from an empty database).
    /// Manual: skipped unless EMS_TEST_SQL is set (see SqlServerFactAttribute); the database is dropped first, so its name must contain "test".
    /// </summary>
    public class SqlMigrationStartupTests
    {
        private static EMSDbContext NewContext()
        {
            var builder = new SqlConnectionStringBuilder(SqlServerFactAttribute.ConnectionString);
            Assert.Contains("test", builder.InitialCatalog, StringComparison.OrdinalIgnoreCase);
            var interceptor = TestDb.NewInterceptor();
            var options = new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer(builder.ConnectionString).AddInterceptors(interceptor).Options;
            return new EMSDbContext(options, interceptor);
        }

        [SqlServerFact]
        public async Task Pending_migrations_are_only_reported_when_off_and_applied_in_order_when_on_and_a_second_run_does_nothing()
        {
            await using var db = NewContext();
            await db.Database.EnsureDeletedAsync();
            var known = db.Database.GetMigrations().ToList();
            Assert.NotEmpty(known);

            // off: the missing migrations are reported and nothing is created
            var reported = await DatabaseMigrator.RunAsync(db, migrate: false, NullLogger.Instance);
            Assert.Equal(known, reported);
            Assert.False(await db.Database.CanConnectAsync());

            // on: all of them are applied
            var applied = await DatabaseMigrator.RunAsync(db, migrate: true, NullLogger.Instance);
            Assert.Equal(known, applied);
            Assert.Equal(known, (await db.Database.GetAppliedMigrationsAsync()).ToList());

            // again: nothing left, nothing happens
            Assert.Empty(await DatabaseMigrator.RunAsync(db, migrate: true, NullLogger.Instance));
            Assert.Empty(await DatabaseMigrator.RunAsync(db, migrate: false, NullLogger.Instance));
        }
    }
}
