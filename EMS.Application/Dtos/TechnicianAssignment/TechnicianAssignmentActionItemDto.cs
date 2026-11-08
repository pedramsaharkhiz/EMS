namespace EMS.Application.Dtos.TechnicianAssignment
{
    public class TechnicianAssignmentActionItemDto
    {
        public long Id { get; set; }
        public long TechnicianAssignmentId { get; set; }
        public long ActionId { get; set; }
        public string? ActionTitle { get; set; }
        public string? ActionCode { get; set; }
        /// <summary>The location-history row this action relates to (optional: most actions do not move the equipment).</summary>
        public long? EquipmentLocationHistoryId { get; set; }
        public DateTime ActionDateTime { get; set; }
        public string? Description { get; set; }
        public long? RecordedByUserId { get; set; }
    }
}
