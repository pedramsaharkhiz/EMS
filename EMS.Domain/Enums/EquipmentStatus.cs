using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Enums
{
    public enum EquipmentStatus
    {
        Unknown = 0,
        InStock = 1,              // موجود در انبار
        InUse = 2,                // در حال استفاده
        Assigned = 3,             // تخصیص داده شده
        UnderMaintenance = 4,     // در تعمیر / سرویس
        OutOfService = 5,         // خارج از سرویس
        Damaged = 6,               // آسیب‌دیده
        Lost = 7,                  // مفقود
        Scrapped = 8,              // اسقاط شده
        Decommissioned = 9        // از رده خارج
    }
}
