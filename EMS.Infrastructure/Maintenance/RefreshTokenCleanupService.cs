using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EMS.Infrastructure.Maintenance
{
    /// <summary>
    /// Background job that deletes refresh tokens that expired or were revoked more than
    /// <see cref="RefreshTokenCleanupOptions.RetentionDays"/> days ago, so the table does not grow forever.
    /// </summary>
    public sealed class RefreshTokenCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<RefreshTokenCleanupOptions> _options;
        private readonly ILogger<RefreshTokenCleanupBackgroundService> _logger;

        public RefreshTokenCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<RefreshTokenCleanupOptions> options,
            ILogger<RefreshTokenCleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = _options.Value;
            if (!options.Enabled)
            {
                _logger.LogInformation("Refresh token cleanup is disabled");
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(Math.Max(0, options.InitialDelayMinutes)), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                        var removed = await CleanupAsync(db, options, DateTime.UtcNow, stoppingToken);
                        if (removed > 0)
                            _logger.LogInformation("Refresh token cleanup removed {RemovedCount} token(s) older than {RetentionDays} day(s)", removed, options.RetentionDays);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Housekeeping must never take the application down; try again next interval.
                        _logger.LogError(ex, "Refresh token cleanup failed");
                    }

                    await Task.Delay(TimeSpan.FromHours(Math.Max(1, options.IntervalHours)), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // application is shutting down
            }
        }

        /// <summary>Deletes expired/revoked tokens past the retention period in batches; returns how many were removed.</summary>
        public static async Task<int> CleanupAsync(EMSDbContext db, RefreshTokenCleanupOptions options, DateTime utcNow, CancellationToken cancellationToken)
        {
            var cutoff = utcNow.AddDays(-Math.Max(0, options.RetentionDays));
            var batchSize = Math.Max(1, options.BatchSize);
            var total = 0;

            while (true)
            {
                var batch = await db.UserRefreshTokens
                    .Where(t => t.ExpiresDateTime < cutoff || (t.RevokedDateTime != null && t.RevokedDateTime < cutoff))
                    .OrderBy(t => t.Id)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    return total;

                db.UserRefreshTokens.RemoveRange(batch);
                await db.SaveChangesAsync(cancellationToken);
                total += batch.Count;

                if (batch.Count < batchSize)
                    return total;
            }
        }
    }
}
