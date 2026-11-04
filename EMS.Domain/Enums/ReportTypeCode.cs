namespace EMS.Domain.Enums
{
    /// <summary>
    /// The report types (نوع گزارش) the business is built on. Each value is also a row of the <c>ReportTypes</c> table with
    /// <c>ReportTypeId == (long)value</c> and <c>Code == value.ToString()</c>. Administrators may rename the titles and add
    /// more types, but these rows cannot be deactivated, deleted or given another code (see <c>SystemCatalogsGuard</c>).
    /// A new value needs a migration that inserts its row; a test guards that.
    /// </summary>
    public enum ReportTypeCode
    {
        Unknown = 0,              // not a row: "no type" (never stored)
        Failure = 1,              // خرابی
        Incident = 2,             // حادثه
        Complaint = 3,            // ثبت شکایت
        Request = 4,              // درخواست رسیدگی
        MaintenanceNeed = 5,      // نیاز به تعمیر / سرویس
        InspectionResult = 6      // نتیجه‌ی بازرسی
    }
}
