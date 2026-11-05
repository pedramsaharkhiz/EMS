using EMS.Domain.Enums;

namespace EMS.Domain
{
    /// <summary>The Persian titles of <see cref="EquipmentOwner"/>: the ONE place they are written (list, detail, Excel and the owners lookup read them).</summary>
    public static class EquipmentOwnerTitles
    {
        public const string Company = "شرکت";
        public const string Bank = "بانک";
        public const string Unknown = "نامشخص";

        public static string TitleOf(EquipmentOwner owner) => owner switch
        {
            EquipmentOwner.Company => Company,
            EquipmentOwner.Bank => Bank,
            _ => Unknown,
        };
    }
}
