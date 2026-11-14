namespace EMS.Domain.Entities
{
    /// <summary>
    /// A failure type that applies to an equipment type (the dropdown of failure types for an equipment shows the types linked to the equipment's type).
    /// A failure type with NO row applies to every equipment type. The row exists exactly while the link does (no soft delete).
    /// </summary>
    public class FailureTypeEquipmentType
    {
        public long FailureTypeId { get; set; }
        public long EquipmentTypeId { get; set; }

        public EquipmentType EquipmentType { get; set; } = null!;
    }
}
