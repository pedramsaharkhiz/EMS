namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// The same change for several equipment at once (POST /api/Equipment/bulk-move). The change is exactly the one of <see cref="EquipmentMoveDto"/>:
    /// every field is optional, <c>null</c> means "leave it", sending a branch (or a supporting department) leaves the other, <c>clearTechnician</c>
    /// takes the technician off, and at least one change must be asked for. All or nothing: if one equipment cannot be changed none is.
    /// </summary>
    public class EquipmentBulkMoveDto
    {
        /// <summary>The equipment to change: 1 to 200, no repeats (inactive equipment can be moved too).</summary>
        public List<long>? EquipmentIds { get; set; }

        public long? CurrentOrganizationId { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public long? TechnicianUserId { get; set; }
        public bool ClearTechnician { get; set; }

        /// <summary>Why they moved: the same text goes into the history entry of each equipment that changed.</summary>
        public string? Description { get; set; }
    }

    /// <summary>What a bulk move did. An equipment that was already in the asked state counts as unchanged (no history entry).</summary>
    public class EquipmentBulkMoveResultDto
    {
        public int Selected { get; set; }
        public int Changed { get; set; }
        public int Unchanged { get; set; }
        /// <summary>The ids of the equipment that actually changed (each has a new history entry).</summary>
        public List<long> ChangedIds { get; set; } = new();
    }
}
