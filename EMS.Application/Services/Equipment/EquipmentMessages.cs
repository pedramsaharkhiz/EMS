namespace EMS.Application.Services.Equipment
{
    /// <summary>The Persian messages of the equipment's uniqueness rules, written once: the service uses them before saving and the unit of work
    /// uses them when the database's unique index is what caught a collision (two requests at the same time).</summary>
    public static class EquipmentMessages
    {
        public const string SerialTaken = "تجهیز دیگری (فعال یا غیرفعال) با این سریال نامبر وجود دارد.";
        public const string AssetTaken = "تجهیز دیگری (فعال یا غیرفعال) با این شماره دارایی وجود دارد.";
    }
}
