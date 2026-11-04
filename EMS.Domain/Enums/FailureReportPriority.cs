namespace EMS.Domain.Enums
{
    /// <summary>
    /// The priority of a failure report, stored as the number (<c>FailureReports.PriorityId</c>, 1 to 5). It has no table: the five
    /// values and their Persian titles are fixed in <see cref="FailureReportPriorities"/>.
    /// </summary>
    public enum FailureReportPriority
    {
        Low = 1,         // کم
        Medium = 2,      // متوسط
        High = 3,        // بالا
        VeryHigh = 4,    // خیلی بالا
        Urgent = 5       // فوری
    }
}
