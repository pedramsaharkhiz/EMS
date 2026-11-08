namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// Changes where an equipment is and/or who has it; every change is recorded in the location history.
    /// <para>
    /// EVERY field is optional and <c>null</c> means "leave it as it is", so one call can change the technician only, the branch only,
    /// the supporting department (ستاد) only, or the technician together with one of the two places. At least one change must be asked for.
    /// </para>
    /// <para>
    /// The equipment is always with exactly one of a branch and a supporting department: sending one of them moves the equipment there and
    /// the other is left automatically; sending both is refused (<c>400</c>).
    /// </para>
    /// </summary>
    public class EquipmentMoveDto
    {
        /// <summary>The branch the equipment goes to (its supporting department is left). Not sent: unchanged.</summary>
        public long? CurrentOrganizationId { get; set; }
        /// <summary>The supporting department (ستاد) the equipment goes to (its branch is left). Not sent: unchanged.</summary>
        public long? CurrentSupportingDepartmentId { get; set; }
        /// <summary>The technician who gets the equipment. Not sent: unchanged (to take the technician off, send <see cref="ClearTechnician"/>).</summary>
        public long? TechnicianUserId { get; set; }
        /// <summary>true: the equipment has no technician any more. Cannot be sent together with <see cref="TechnicianUserId"/>.</summary>
        public bool ClearTechnician { get; set; }
        /// <summary>Why the equipment moved (kept in the history entry).</summary>
        public string? Description { get; set; }
        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
