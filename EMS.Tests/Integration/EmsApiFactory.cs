using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Logging:AuditTrail:Enabled"] = "true",
                    // Write the real file sinks into a per-run directory instead of the working directory.
                    ["Serilog:WriteTo:1:Args:path"] = Path.Combine(LogDirectory, "ems-.log"),
                    ["Serilog:WriteTo:2:Args:configureLogger:WriteTo:0:Args:path"] = Path.Combine(LogDirectory, "ems-security-.log")
                });
            });

            builder.ConfigureServices(services =>
            {
                // Serilog's ReadFrom.Services picks this sink up.
                services.AddSingleton<ILogEventSink>(Sink);

                services.RemoveAll<DbContextOptions<EMSDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<EMSDbContext>>();
                services.AddDbContext<EMSDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    options.AddInterceptors(
                        sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                        sp.GetRequiredService<SlowQueryLoggingInterceptor>());
                });
            });
        }
    }
}
