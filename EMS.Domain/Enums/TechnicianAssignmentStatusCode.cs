namespace EMS.Domain.Enums
{
    /// <summary>
    /// The statuses of a technician assignment (ارجاع به تکنسین). Each value is also a row of the <c>TechnicianAssignmentStatuses</c> table with
    /// <c>TechnicianAssignmentStatusId == (long)value</c> and <c>Code == value.ToString()</c>.
    /// Open (not final): Assigned, Accepted, InProgress. Final: Rejected, Completed, Cancelled.
    /// </summary>
    public enum TechnicianAssignmentStatusCode
    {
        Unknown = 0,      // not a row: "no status" (never stored)
        Assigned = 1,     // ارجاع‌شده
        Accepted = 2,     // پذیرفته‌شده
        Rejected = 3,     // ردشده
        InProgress = 4,   // در حال انجام
        Completed = 5,    // تکمیل‌شده
        Cancelled = 6     // لغوشده
    }
}
