using EMS.Infrastructure.Persistence;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace EMS.Tests.Unit
{
    public class DatabaseMigratorTests
    {
        // the settings files may contain comments
        private static readonly JsonDocumentOptions Lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        [Fact]
        public async Task The_in_memory_database_has_no_schema_history_so_nothing_happens_in_either_mode()
        {
            var interceptor = TestDb.NewInterceptor();
            var options = new DbContextOptionsBuilder<EMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            await using var context = new EMSDbContext(options, interceptor);

            Assert.Empty(await DatabaseMigrator.RunAsync(context, migrate: false, NullLogger.Instance));
            Assert.Empty(await DatabaseMigrator.RunAsync(context, migrate: true, NullLogger.Instance));
        }

        [Fact]
        public void Migrating_at_start_up_is_off_unless_the_setting_is_switched_on_explicitly()
        {
            var root = AppContext.BaseDirectory;
            while (root is not null && !File.Exists(Path.Combine(root, "EMS.slnx")))
                root = Path.GetDirectoryName(root);
            Assert.NotNull(root);

            foreach (var file in new[] { "appsettings.json", "appsettings.Production.json", "appsettings.Development.json" })
            {
                using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!, "EMS.Presentation", file)), Lenient);
                if (json.RootElement.TryGetProperty("Database", out var database) && database.TryGetProperty("MigrateOnStartup", out var value))
                    Assert.False(value.GetBoolean(), $"{file} must not switch automatic migration on");
            }
            using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!, "EMS.Presentation", "appsettings.json")), Lenient);
            Assert.False(baseline.RootElement.GetProperty("Database").GetProperty("MigrateOnStartup").GetBoolean());
        }
    }
}
