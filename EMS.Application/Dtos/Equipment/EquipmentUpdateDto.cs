using EMS.Domain.Enums;

namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// Replaces the editable fields of an equipment: whatever is not sent is cleared (serial, asset number, description), and
    /// exactly one of type / model must be sent. The location is not part of it: change it with the move action.
    /// </summary>
    public class EquipmentUpdateDto
    {
        public EquipmentOwner Owner { get; set; }
        public bool HasConflict { get; set; } = false;
        public string YaasCode { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? AssetNumber { get; set; }
        public long? EquipmentTypeId { get; set; }
        public long? EquipmentModelId { get; set; }
        public long EquipmentStatusId { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
