namespace EMS.Domain.Enums
{
    /// <summary>
    /// Who an equipment is with right now. It is never stored: it follows from which of the two location columns is filled
    /// (exactly one of them always is, see the CHECK constraint CK_Equipments_Location_ExactlyOne).
    /// </summary>
    public enum EquipmentLocationKind
    {
        Organization = 1,          // شعبه
        SupportingDepartment = 2,  // ستاد (دپارتمان پشتیبانی)
    }
}
