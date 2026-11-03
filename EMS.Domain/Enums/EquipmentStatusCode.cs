namespace EMS.Domain.Enums
{
    /// <summary>
    /// The statuses the code itself relies on. Each value is also a row of the <c>EquipmentStatuses</c> table with
    /// <c>EquipmentStatusId == (long)value</c> and <c>Code == value.ToString()</c> (seeded by the migration, see
    /// <see cref="EquipmentStatusCatalog"/>), so the code can say <c>EquipmentStatusCode.Lost</c> while administrators still
    /// manage the table: they may rename the titles and add more statuses, but these rows cannot be deactivated,
    /// deleted or given another code. A new value needs a migration that inserts its row; a test guards that.
    /// </summary>
    public enum EquipmentStatusCode
    {
        Unknown = 0,              // not a row: "no status" (never stored)
        InStock = 1,              // موجود در انبار
        InUse = 2,                // در حال استفاده
        Assigned = 3,             // تخصیص داده شده
        UnderMaintenance = 4,     // در تعمیر / سرویس
        OutOfService = 5,         // خارج از سرویس
        Damaged = 6,              // آسیب‌دیده
        Lost = 7,                 // مفقود
        Scrapped = 8,             // اسقاط شده
        Decommissioned = 9        // از رده خارج
    }
}
