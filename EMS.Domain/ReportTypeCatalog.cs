using EMS.Domain.Enums;

namespace EMS.Domain
{
    /// <summary>The seeded system rows of the <c>ReportTypes</c> table and the rules that identify them.</summary>
    public static class ReportTypeCatalog
    {
        /// <summary>Initial Persian titles. Administrators may rename them later; the migration only inserts them once.</summary>
        public static readonly IReadOnlyDictionary<ReportTypeCode, string> Titles = new Dictionary<ReportTypeCode, string>
        {
            [ReportTypeCode.Failure] = "خرابی",
            [ReportTypeCode.Incident] = "حادثه",
            [ReportTypeCode.Complaint] = "ثبت شکایت",
            [ReportTypeCode.Request] = "درخواست رسیدگی",
            [ReportTypeCode.MaintenanceNeed] = "نیاز به تعمیر / سرویس",
            [ReportTypeCode.InspectionResult] = "نتیجه‌ی بازرسی",
        };

        public static long IdOf(ReportTypeCode code) => (long)code;

        public static bool IsSystemCode(string? code) => TryGetSystemCode(code, out _);

        public static bool TryGetSystemCode(string? code, out ReportTypeCode result)
        {
            result = ReportTypeCode.Unknown;
            return !string.IsNullOrWhiteSpace(code)
                   && Enum.TryParse(code.Trim(), ignoreCase: true, out result)
                   && Titles.ContainsKey(result);
        }

        /// <summary>Is the row with this id and code one of the seeded system rows?</summary>
        public static bool IsSystemRow(long id, string? code) =>
            TryGetSystemCode(code, out var parsed) && id == IdOf(parsed) && string.Equals(code!.Trim(), parsed.ToString(), StringComparison.Ordinal);
    }
}
