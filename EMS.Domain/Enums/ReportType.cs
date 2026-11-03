using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Enums
{
    public enum ReportType
    {
        Unknown = 0,
        Failure = 1,              // خرابی
        Incident = 2,              // حادثه
        Complaint = 3,             // ثبت شکایت / گزارش
        Request = 4,               // درخواست رسیدگی
        MaintenanceNeed = 5,       // نیاز به تعمیر / سرویس
        InspectionResult = 6       // نتیجه بازرسی
    }
}
