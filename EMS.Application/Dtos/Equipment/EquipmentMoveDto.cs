namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// Moves an equipment to a new location; every move is recorded in the location history. The equipment goes to a branch OR to a
    /// supporting department: exactly one of <c>currentOrganizationId</c> and <c>currentSupportingDepartmentId</c>.
    /// </summary>
    public class EquipmentMoveDto
    {
        public long? CurrentOrganizationId { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public long? TechnicianUserId { get; set; }

        /// <summary>Why the equipment moved (kept in the history entry).</summary>
        public string? Description { get; set; }

        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
