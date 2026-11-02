namespace EMS.Domain.Entities
{
    /// <summary>
    /// Data-level audit trail: one row per inserted / updated / deleted entity, with old and new values.
    /// Deliberately does not derive from <see cref="BaseEntity"/> (it must not be audited itself).
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }

        /// <summary>Database table the change applies to.</summary>
        public string TableName { get; set; } = null!;

        /// <summary>Primary key value(s) of the changed row (composite keys are joined with '|').</summary>
        public string EntityId { get; set; } = null!;

        /// <summary>Insert, Update or Delete.</summary>
        public string Action { get; set; } = null!;

        /// <summary>JSON object with the previous values (Update/Delete). Sensitive columns are masked.</summary>
        public string? OldValues { get; set; }

        /// <summary>JSON object with the new values (Insert/Update). Sensitive columns are masked.</summary>
        public string? NewValues { get; set; }

        /// <summary>Comma separated names of the columns that changed (Update).</summary>
        public string? ChangedColumns { get; set; }

        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }

        /// <summary>The X-Correlation-ID of the request, to join with the application logs.</summary>
        public string? CorrelationId { get; set; }

        public DateTime TimestampUtc { get; set; }
    }
}
