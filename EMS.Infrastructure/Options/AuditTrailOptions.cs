namespace EMS.Infrastructure.Options
{
    /// <summary>
    /// Settings for the database audit trail (<c>AuditLogs</c> table).
    /// Bound from configuration section <c>Logging:AuditTrail</c>.
    /// The table must exist (apply the AddAuditLog migration) before this is enabled.
    /// </summary>
    public sealed class AuditTrailOptions
    {
        public const string SectionName = "Logging:AuditTrail";

        public bool Enabled { get; set; }
    }
}
