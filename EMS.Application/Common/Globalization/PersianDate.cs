using System.Globalization;

namespace EMS.Application.Common.Globalization
{
    /// <summary>
    /// The one place that turns dates into Persian (Jalali) text. Every DateTime in the database is UTC (see
    /// <c>UtcDateTimeConverter</c>), so a value is first moved to Tehran time and then written as <c>yyyy/MM/dd</c> or
    /// <c>yyyy/MM/dd HH:mm</c> with zero padding (so the text also sorts correctly) and Latin digits (so it stays searchable).
    /// A <c>DateTime</c> whose Kind is Unspecified is read as UTC, the way the database hands values back.
    /// The Excel exporter uses this for every date column automatically; use it directly for anything else that
    /// must show a Jalali date. The API itself keeps ISO 8601.
    /// </summary>
    public static class PersianDate
    {
        private static readonly PersianCalendar Calendar = new();
        private static readonly Lazy<TimeZoneInfo> Tehran = new(ResolveTehran);

        /// <summary><c>1405/08/12</c> for a UTC moment (the date as seen in Tehran).</summary>
        public static string ToDate(DateTime value) => FormatLocal(ToTehranTime(value), withTime: false);

        /// <summary><c>1405/08/12 14:30</c> for a UTC moment (Tehran time).</summary>
        public static string ToDateTime(DateTime value) => FormatLocal(ToTehranTime(value), withTime: true);

        public static string ToDate(DateTimeOffset value) => ToDate(value.UtcDateTime);
        public static string ToDateTime(DateTimeOffset value) => ToDateTime(value.UtcDateTime);

        /// <summary>A calendar date has no time zone: it is converted as is.</summary>
        public static string ToDate(DateOnly value) => FormatLocal(value.ToDateTime(TimeOnly.MinValue), withTime: false);

        /// <summary>Empty text for null, so a nullable column needs no special handling.</summary>
        public static string ToDate(DateTime? value) => value.HasValue ? ToDate(value.Value) : string.Empty;
        public static string ToDateTime(DateTime? value) => value.HasValue ? ToDateTime(value.Value) : string.Empty;

        /// <summary>The same moment as Tehran wall-clock time.</summary>
        public static DateTime ToTehranTime(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, Tehran.Value);
        }

        private static string FormatLocal(DateTime local, bool withTime)
        {
            // The Persian calendar starts in 622 AD; outside its range fall back to ISO text instead of throwing.
            if (local < Calendar.MinSupportedDateTime || local > Calendar.MaxSupportedDateTime)
                return local.ToString(withTime ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var date = $"{Calendar.GetYear(local):0000}/{Calendar.GetMonth(local):00}/{Calendar.GetDayOfMonth(local):00}";
            return withTime ? $"{date} {local.Hour:00}:{local.Minute:00}" : date;
        }

        private static TimeZoneInfo ResolveTehran()
        {
            foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            // A host without time zone data (e.g. a minimal container image): Iran has used a fixed UTC+03:30 since 2022.
            return TimeZoneInfo.CreateCustomTimeZone("Asia/Tehran", TimeSpan.FromHours(3.5), "Tehran", "Tehran");
        }
    }
}
