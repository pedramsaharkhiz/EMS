using EMS.Domain.Enums;

namespace EMS.Domain
{
    /// <summary>The seeded system rows of the <c>FailureReportStatuses</c> table and the rules that identify them.</summary>
    public static class FailureReportStatusCatalog
    {
        /// <summary>Initial Persian titles. Administrators may rename them later; the migration only inserts them once.</summary>
        public static readonly IReadOnlyDictionary<FailureReportStatusCode, string> Titles = new Dictionary<FailureReportStatusCode, string>
        {
            [FailureReportStatusCode.Open] = "باز",
            [FailureReportStatusCode.InProgress] = "در حال رسیدگی",
            [FailureReportStatusCode.Closed] = "بسته",
        };

        public static long IdOf(FailureReportStatusCode code) => (long)code;

        public static bool IsSystemCode(string? code) => TryGetSystemCode(code, out _);

        public static bool TryGetSystemCode(string? code, out FailureReportStatusCode result)
        {
            result = FailureReportStatusCode.Unknown;
            return !string.IsNullOrWhiteSpace(code)
                   && Enum.TryParse(code.Trim(), ignoreCase: true, out result)
                   && Titles.ContainsKey(result);
        }

        /// <summary>Is the row with this id and code one of the seeded system rows?</summary>
        public static bool IsSystemRow(long id, string? code) =>
            TryGetSystemCode(code, out var parsed) && id == IdOf(parsed) && string.Equals(code!.Trim(), parsed.ToString(), StringComparison.Ordinal);
    }
}
