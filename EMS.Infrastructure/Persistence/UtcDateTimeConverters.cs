using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EMS.Infrastructure.Persistence
{
    /// <summary>
    /// Every DateTime in the database is UTC (the audit interceptor and the services write <c>DateTime.UtcNow</c>), but SQL
    /// Server gives back values with <c>Kind = Unspecified</c>, which the JSON serializer writes without a "Z". A browser then
    /// reads such a string as local time and shows it shifted. Marking what is read as UTC makes the API return
    /// ISO 8601 with "Z" for every date. No schema change: only the CLR value is affected.
    /// </summary>
    public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter()
            : base(
                v => v.HasValue && v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }
}
