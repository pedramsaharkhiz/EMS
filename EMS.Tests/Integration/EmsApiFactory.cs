using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Boots the real API (real pipeline, real Serilog configuration from appsettings.json) against an
    /// in-memory database, capturing everything that is logged.
    /// </summary>
    public sealed class EmsApiFactory : WebApplicationFactory<Program>
    {
        public CapturingSink Sink { get; } = new();

        // Kept short on purpose: Serilog's shared file sink derives a named mutex from the full path,
        // and Unix limits the length of such names.
        public string LogDirectory { get; } =
            Path.Combine(Path.GetTempPath(), "ems-t-" + Guid.NewGuid().ToString("N")[..8]);

        private readonly string _databaseName = Guid.NewGuid().ToString("N");
        // One root shared by every context of this factory: an in-memory store is only shared between contexts built from
        // the same root, and the seeding context below is configured differently from the application's.
        private readonly InMemoryDatabaseRoot _databaseRoot = new();
        private readonly object _seedLock = new();
        private bool _seedDataCreated;

        /// <summary>
        /// The in-memory database is not built by migrations. EnsureCreated applies the model's seed data (HasData), which is how
        /// the real database gets it from the migration (e.g. the system equipment statuses). It has to happen before the first
        /// write (the administrator seeded at startup), so it is done when the first context is configured.
        /// </summary>
        private void EnsureSeedDataCreated(IServiceProvider services)
        {
            lock (_seedLock)
            {
                if (_seedDataCreated)
                    return;

                _seedDataCreated = true;
                using var context = new EMSDbContext(
                    new DbContextOptionsBuilder<EMSDbContext>().UseInMemoryDatabase(_databaseName, _databaseRoot).Options,
                    services.GetRequiredService<AuditSaveChangesInterceptor>());
                context.Database.EnsureCreated();
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:AuditTrail:Enabled"] = "true",
                    // The allowed browser origins are configuration, not code (the test environment has no appsettings.<env>.json).
                    ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                    ["Cors:AllowedOrigins:1"] = "http://192.168.200.62",
                    // Tests start from empty catalogs whatever appsettings says (the geography seeder is exercised explicitly).
                    ["Seed:Geography:Enabled"] = "false",
                    // Write the real file sinks into a per-run directory instead of the working directory.
                    ["Serilog:WriteTo:1:Args:path"] = Path.Combine(LogDirectory, "ems-.log"),
                    ["Serilog:WriteTo:2:Args:configureLogger:WriteTo:0:Args:path"] = Path.Combine(LogDirectory, "ems-security-.log")
                });
            });

            builder.ConfigureServices(services =>
            {
                // Serilog's ReadFrom.Services picks this sink up.
                services.AddSingleton<ILogEventSink>(Sink);

                // the in-memory database cannot run the raw SQL that streams attachment files (see InMemoryAttachmentBlobStore)
                services.RemoveAll<EMS.Application.Interfaces.Attachment.IAttachmentBlobStore>();
                services.AddSingleton<EMS.Application.Interfaces.Attachment.IAttachmentBlobStore, EMS.Tests.Support.InMemoryAttachmentBlobStore>();

                services.RemoveAll<DbContextOptions<EMSDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<EMSDbContext>>();
                services.AddDbContext<EMSDbContext>((sp, options) =>
                {
                    EnsureSeedDataCreated(sp);
                    options.UseInMemoryDatabase(_databaseName, _databaseRoot);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    options.AddInterceptors(
                        sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                        sp.GetRequiredService<SlowQueryLoggingInterceptor>());
                });
            });
        }
    }
}
