using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Enums
{
    public enum TechnicianAssignmentStatus
    {
        Unknown = 0,
        Pending = 1,               // در انتظار
        Assigned = 2,              // تخصیص داده شده
        Accepted = 3,              // پذیرفته شده
        Rejected = 4,              // رد شده
        InProgress = 5,            // در حال انجام
        Completed = 6,             // تکمیل شده
        Cancelled = 7,             // لغو شده
        Reassigned = 8             // ارجاع مجدد
    }
}
