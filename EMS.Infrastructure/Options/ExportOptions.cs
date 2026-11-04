namespace EMS.Infrastructure.Options
{
    /// <summary>The <c>Export</c> section of appsettings: limits that protect the server from very large or too many exports.</summary>
    public sealed class ExportOptions
    {
        public const string SectionName = "Export";

        /// <summary>The most rows one export may contain; more is refused (400 export_too_large) rather than truncated.</summary>
        public int MaxRows { get; set; } = 50_000;

        /// <summary>How many exports may run at the same time in this process; more is refused (429 export_busy).</summary>
        public int MaxConcurrent { get; set; } = 2;
    }
}
