using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Persistence
{
    /// <summary>
    /// What the application does about the database schema when it starts. By default NOTHING is changed: the operator applies the migrations
    /// (the idempotent scripts in docs/sql, or "dotnet ef database update") and the application only warns when some are missing.
    /// With <c>Database:MigrateOnStartup = true</c> the application applies the pending migrations itself, before the seeders run.
    /// See docs/adr/0013-database-migrations-are-applied-by-the-operator.md.
    /// </summary>
    public static class DatabaseMigrator
    {
        public const string SettingName = "Database:MigrateOnStartup";

        /// <returns>The migrations that were applied now, or the ones still pending when <paramref name="migrate"/> is false (empty when the schema is current).</returns>
        public static async Task<IReadOnlyList<string>> RunAsync(EMSDbContext context, bool migrate, ILogger logger, CancellationToken cancellationToken = default)
        {
            // The in-memory database used by the tests has no schema history.
            if (!context.Database.IsRelational())
                return Array.Empty<string>();

            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count == 0)
                return pending;

            if (!migrate)
            {
                logger.LogWarning(
                    "The database is missing {Count} migration(s): {Migrations}. Apply them (docs/sql scripts or 'dotnet ef database update') or set {Setting}=true. Start-up will probably fail until then",
                    pending.Count, string.Join(", ", pending), SettingName);
                return pending;
            }

            logger.LogWarning("Applying {Count} pending database migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
            // A failing migration stops the start-up on purpose: running on a half-migrated schema is worse than not running.
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied");
            return pending;
        }
    }
}
