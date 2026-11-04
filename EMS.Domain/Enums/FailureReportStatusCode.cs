namespace EMS.Domain.Enums
{
    /// <summary>
    /// The statuses of a failure report. Each value is also a row of the <c>FailureReportStatuses</c> table with
    /// <c>FailureReportStatusId == (long)value</c> and <c>Code == value.ToString()</c>; they cannot be deactivated, deleted or
    /// re-coded (see <c>SystemCatalogsGuard</c>). A new report starts as <see cref="Open"/>.
    /// </summary>
    public enum FailureReportStatusCode
    {
        Unknown = 0,      // not a row: "no status" (never stored)
        Open = 1,         // باز
        InProgress = 2,   // در حال رسیدگی
        Closed = 3        // بسته
    }
}
