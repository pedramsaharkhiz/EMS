namespace EMS.Application.Dtos.Equipment
{
    /// <summary>Moves an equipment to a new location; every move is recorded in the location history.</summary>
    public class EquipmentMoveDto
    {
        public long CurrentEquipmentLocationTypeId { get; set; }
        public long? CurrentOrganizationId { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public long? TechnicianUserId { get; set; }

        /// <summary>Why the equipment moved (kept in the history entry).</summary>
        public string? Description { get; set; }

        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
