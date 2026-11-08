using EMS.Domain.Enums;

namespace EMS.Domain
{
    /// <summary>The Persian titles of <see cref="EquipmentLocationKind"/>: the ONE place they are written (list, detail, history and Excel read them).</summary>
    public static class EquipmentLocationKindTitles
    {
        public const string Organization = "شعبه";
        public const string SupportingDepartment = "ستاد";
        public const string Unknown = "نامشخص";

        public static string TitleOf(EquipmentLocationKind kind) => kind switch
        {
            EquipmentLocationKind.Organization => Organization,
            EquipmentLocationKind.SupportingDepartment => SupportingDepartment,
            _ => Unknown,
        };
    }
}
