namespace EMS.Infrastructure.Options
{
    /// <summary>Periodic removal of refresh tokens that are long expired or revoked. Section: Maintenance:RefreshTokenCleanup.</summary>
    public sealed class RefreshTokenCleanupOptions
    {
        public const string SectionName = "Maintenance:RefreshTokenCleanup";

        public bool Enabled { get; set; } = true;

        /// <summary>Tokens are kept this long after they expired or were revoked (useful for investigations).</summary>
        public int RetentionDays { get; set; } = 30;

        public int IntervalHours { get; set; } = 24;

        /// <summary>Delay before the first run after start-up, so start-up is never slowed down.</summary>
        public int InitialDelayMinutes { get; set; } = 5;

        public int BatchSize { get; set; } = 1000;
    }
}
